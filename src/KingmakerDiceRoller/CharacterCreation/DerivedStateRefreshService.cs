using System;
using System.Collections;
using System.Reflection;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class DerivedAllowanceSnapshot
    {
        internal DerivedAllowanceSnapshot(
            int intelligenceSkillPoints,
            int progressionTotal,
            int totalSkillPoints,
            int skillPointsRemaining,
            SpellAllowanceSnapshot[] spells)
        {
            IntelligenceSkillPoints = intelligenceSkillPoints;
            ProgressionTotalIntelligenceSkillPoints = progressionTotal;
            TotalSkillPoints = totalSkillPoints;
            SkillPointsRemaining = skillPointsRemaining;
            Spells = spells;
        }

        public int IntelligenceSkillPoints { get; }
        public int ProgressionTotalIntelligenceSkillPoints { get; }
        public int TotalSkillPoints { get; }
        public int SkillPointsRemaining { get; }
        internal SpellAllowanceSnapshot[] Spells { get; }
    }

    internal sealed class SpellAllowanceSnapshot
    {
        internal SpellAllowanceSnapshot(object identity, int extraMaxLevel, Array[] selections)
        {
            Identity = identity;
            ExtraMaxLevel = extraMaxLevel;
            Selections = selections;
        }

        internal object Identity { get; }
        internal int ExtraMaxLevel { get; }
        internal Array[] Selections { get; }
    }

    // Kingmaker caches ability-derived allowances on LevelUpState. Exact 2.1.7b IL proves:
    //   OnApplyAction() recomputes TotalSkillPoints from the cached IntelligenceSkillPoints
    //   field and refreshes SpellSelectionData.UpdateMaxLevelSpells per spellbook;
    //   IntelligenceSkillPoints itself is written only by the ApplySkillPoints action, which
    //   reads the unit's live Intelligence and records the granted total on the progression.
    // Staging a rolled array therefore also has to redo that native bookkeeping, or the
    // allowances validated in the UI diverge from the ones the authoritative replay computes.
    public sealed class DerivedStateRefreshService
    {
        public bool TryCapture(
            object state,
            object unit,
            KingmakerContracts contracts,
            out DerivedAllowanceSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (state == null || unit == null || contracts == null ||
                contracts.DerivedAllowances == null)
            {
                error = "stage=readiness; exact derived-allowance contracts and live objects are required.";
                return false;
            }
            string stage = "read live skill caches";
            try
            {
                object progression = ReflectionAccess.Read(contracts.UnitDescriptorProgressionMember, unit);
                if (progression == null)
                    throw new InvalidOperationException("The live preview progression is unavailable.");
                int granted = ReadInt(contracts.LevelUpStateIntelligenceSkillPointsMember, state);
                int progressionTotal = ReadInt(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember, progression);
                int totalSkill = ReadInt(contracts.DerivedAllowances.TotalSkillPoints, state);
                int remainingSkill = ReadInt(contracts.LevelUpStateSkillPointsRemainingMember, state);
                stage = "capture spell allowances";
                IList spellList = ReflectionAccess.Read(contracts.DerivedAllowances.SpellSelections, state) as IList;
                var spells = new SpellAllowanceSnapshot[spellList == null ? 0 : spellList.Count];
                for (int index = 0; index < spells.Length; index++)
                {
                    object selection = spellList[index];
                    if (selection == null)
                        throw new InvalidOperationException("A spell selection is null.");
                    int extraMax = ReadInt(contracts.DerivedAllowances.SpellExtraMaxLevel, selection);
                    Array levels = ReflectionAccess.Read(
                        contracts.DerivedAllowances.SpellLevelCount, selection) as Array;
                    if (levels == null)
                        throw new InvalidOperationException("SpellSelectionData.LevelCount is unavailable.");
                    var selected = new Array[levels.Length];
                    for (int level = 0; level < levels.Length; level++)
                    {
                        object levelState = levels.GetValue(level);
                        if (levelState == null) continue;
                        Array values = ReflectionAccess.Read(
                            contracts.DerivedAllowances.SelectedSpells, levelState) as Array;
                        if (values == null)
                            throw new InvalidOperationException("A spell selection level has no slot array.");
                        selected[level] = (Array)values.Clone();
                    }
                    spells[index] = new SpellAllowanceSnapshot(selection, extraMax, selected);
                }
                snapshot = new DerivedAllowanceSnapshot(
                    granted, progressionTotal, totalSkill, remainingSkill, spells);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = DescribeFailure(stage, exception);
                return false;
            }
        }

        public bool TryRefresh(
            object state,
            object unit,
            KingmakerContracts contracts,
            out DerivedAllowanceSnapshot previous,
            out int grantedIntelligenceSkillPoints,
            out string error)
        {
            grantedIntelligenceSkillPoints = 0;
            if (!TryCapture(state, unit, contracts, out previous, out error)) return false;
            string stage = "read live binding";
            try
            {
                int nextLevel = ReadInt(contracts.LevelUpStateNextLevelMember, state);
                object progression = ReflectionAccess.Read(contracts.UnitDescriptorProgressionMember, unit);
                if (progression == null)
                    throw new InvalidOperationException("The live preview progression is unavailable.");
                int baseline = previous.ProgressionTotalIntelligenceSkillPoints -
                    previous.IntelligenceSkillPoints;
                stage = "native Intelligence skill-point calculation";
                object recomputed = contracts.LevelUpHelperGetTotalIntelligenceSkillPointsMethod.Invoke(
                    null, new[] { unit, nextLevel });
                if (!(recomputed is int))
                    throw new InvalidOperationException("LevelUpHelper did not return an Int32 value.");
                int total = (int)recomputed;
                int granted = total - baseline;
                stage = "write skill-point caches";
                ReflectionAccess.Write(contracts.LevelUpStateIntelligenceSkillPointsMember, state, granted);
                ReflectionAccess.Write(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember, progression, total);
                stage = "native LevelUpState.OnApplyAction";
                contracts.LevelUpStateOnApplyActionMethod.Invoke(state, null);
                stage = "verify refreshed skill and spell allowances";
                DerivedAllowanceSnapshot refreshed;
                string readError;
                if (!TryCapture(state, unit, contracts, out refreshed, out readError))
                    throw new InvalidOperationException(readError);
                if (refreshed.IntelligenceSkillPoints != granted ||
                    refreshed.ProgressionTotalIntelligenceSkillPoints != total)
                    throw new InvalidOperationException("The native skill-point cache writes did not persist.");
                grantedIntelligenceSkillPoints = granted;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = DescribeFailure(stage, exception);
                return false;
            }
        }

        public bool TryRestore(
            DerivedAllowanceSnapshot previous,
            object state,
            object unit,
            KingmakerContracts contracts,
            out string error)
        {
            if (previous == null || state == null || unit == null || contracts == null ||
                contracts.DerivedAllowances == null)
            {
                error = "stage=readiness; the derived-allowance rollback requires a snapshot and exact live objects.";
                return false;
            }
            string stage = "read live binding";
            try
            {
                object progression = ReflectionAccess.Read(contracts.UnitDescriptorProgressionMember, unit);
                if (progression == null)
                    throw new InvalidOperationException("The live preview progression is unavailable.");
                stage = "restore skill-point caches";
                ReflectionAccess.Write(
                    contracts.LevelUpStateIntelligenceSkillPointsMember,
                    state,
                    previous.IntelligenceSkillPoints);
                ReflectionAccess.Write(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember,
                    progression,
                    previous.ProgressionTotalIntelligenceSkillPoints);
                stage = "native rollback LevelUpState.OnApplyAction";
                contracts.LevelUpStateOnApplyActionMethod.Invoke(state, null);
                stage = "verify restored skill caches";
                DerivedAllowanceSnapshot restored;
                string readError;
                if (!TryCapture(state, unit, contracts, out restored, out readError))
                    throw new InvalidOperationException(readError);
                if (restored.IntelligenceSkillPoints != previous.IntelligenceSkillPoints ||
                    restored.ProgressionTotalIntelligenceSkillPoints !=
                        previous.ProgressionTotalIntelligenceSkillPoints ||
                    restored.TotalSkillPoints != previous.TotalSkillPoints ||
                    restored.SkillPointsRemaining != previous.SkillPointsRemaining)
                    throw new InvalidOperationException("Native skill allowances differ from the prior committed state.");

                stage = "verify restored spell allowances";
                IList spellList = ReflectionAccess.Read(contracts.DerivedAllowances.SpellSelections, state) as IList;
                if ((spellList == null ? 0 : spellList.Count) != previous.Spells.Length)
                    throw new InvalidOperationException("The number of native spell selections changed.");
                for (int index = 0; index < previous.Spells.Length; index++)
                {
                    SpellAllowanceSnapshot saved = previous.Spells[index];
                    object selection = spellList[index];
                    if (!ReferenceEquals(selection, saved.Identity) ||
                        ReadInt(contracts.DerivedAllowances.SpellExtraMaxLevel, selection) != saved.ExtraMaxLevel)
                        throw new InvalidOperationException("A native spell selection or extra-level allowance changed.");
                    Array levels = ReflectionAccess.Read(
                        contracts.DerivedAllowances.SpellLevelCount, selection) as Array;
                    if (levels == null || levels.Length != saved.Selections.Length)
                        throw new InvalidOperationException("The native spell level count changed.");
                    for (int level = 0; level < levels.Length; level++)
                    {
                        Array selectedBefore = saved.Selections[level];
                        object levelState = levels.GetValue(level);
                        if (selectedBefore == null && levelState == null) continue;
                        if (selectedBefore == null || levelState == null)
                            throw new InvalidOperationException("A native spell level state changed.");
                        Array selectedNow = ReflectionAccess.Read(
                            contracts.DerivedAllowances.SelectedSpells, levelState) as Array;
                        if (selectedNow == null || selectedNow.Length != selectedBefore.Length)
                            throw new InvalidOperationException("Native spell slot allowance did not restore.");
                        // The prior choices were valid under the restored scores. Native
                        // OnApplyAction has already proved the slot count; put back only
                        // choices a failed candidate may have displaced.
                        Array.Copy(selectedBefore, selectedNow, selectedBefore.Length);
                    }
                }
                stage = "verify restored spell choices";
                if (!TryCapture(state, unit, contracts, out restored, out readError) ||
                    !MatchesSpells(previous, restored))
                    throw new InvalidOperationException(readError ??
                        "Native spell selections did not match the prior committed state.");
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = DescribeFailure(stage, exception);
                return false;
            }
        }

        private static bool MatchesSpells(
            DerivedAllowanceSnapshot previous, DerivedAllowanceSnapshot current)
        {
            if (current == null || previous.Spells.Length != current.Spells.Length) return false;
            for (int index = 0; index < previous.Spells.Length; index++)
            {
                SpellAllowanceSnapshot before = previous.Spells[index];
                SpellAllowanceSnapshot after = current.Spells[index];
                if (!ReferenceEquals(before.Identity, after.Identity) ||
                    before.ExtraMaxLevel != after.ExtraMaxLevel ||
                    before.Selections.Length != after.Selections.Length) return false;
                for (int level = 0; level < before.Selections.Length; level++)
                {
                    Array earlier = before.Selections[level];
                    Array later = after.Selections[level];
                    if (earlier == null || later == null)
                    {
                        if (earlier != later) return false;
                        continue;
                    }
                    if (earlier.Length != later.Length) return false;
                    for (int slot = 0; slot < earlier.Length; slot++)
                        if (!ReferenceEquals(earlier.GetValue(slot), later.GetValue(slot))) return false;
                }
            }
            return true;
        }

        private static int ReadInt(MemberInfo member, object owner)
        {
            object value = ReflectionAccess.Read(member, owner);
            if (!(value is int))
                throw new InvalidOperationException(member.Name + " is not an Int32 live allowance.");
            return (int)value;
        }

        private static string DescribeFailure(string stage, Exception exception)
        {
            Exception actual = exception;
            while (actual is TargetInvocationException && actual.InnerException != null)
                actual = actual.InnerException;
            return "stage=" + stage + "; " + actual;
        }
    }
}
