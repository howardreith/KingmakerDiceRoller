using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class DerivedAllowanceSnapshot
    {
        internal DerivedAllowanceSnapshot(int intelligenceSkillPoints, int progressionTotal)
        {
            IntelligenceSkillPoints = intelligenceSkillPoints;
            ProgressionTotalIntelligenceSkillPoints = progressionTotal;
        }

        public int IntelligenceSkillPoints { get; }
        public int ProgressionTotalIntelligenceSkillPoints { get; }
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
        public bool TryRefresh(
            object state,
            object unit,
            KingmakerContracts contracts,
            out DerivedAllowanceSnapshot previous,
            out int grantedIntelligenceSkillPoints,
            out string error)
        {
            previous = null;
            grantedIntelligenceSkillPoints = 0;
            if (state == null || unit == null || contracts == null)
            {
                error = "The derived-state refresh requires a live state, unit, and Kingmaker contracts.";
                return false;
            }

            try
            {
                object nextLevelValue = ReflectionAccess.Read(contracts.LevelUpStateNextLevelMember, state);
                object grantedValue = ReflectionAccess.Read(
                    contracts.LevelUpStateIntelligenceSkillPointsMember,
                    state);
                if (!(nextLevelValue is int) || !(grantedValue is int))
                {
                    error = "The live LevelUpState derived-allowance fields are not Int32 values.";
                    return false;
                }
                int nextLevel = (int)nextLevelValue;
                int currentGranted = (int)grantedValue;

                object progression = ReflectionAccess.Read(contracts.UnitDescriptorProgressionMember, unit);
                if (progression == null)
                {
                    error = "The live preview progression is unavailable for the derived-state refresh.";
                    return false;
                }
                object totalGrantedValue = ReflectionAccess.Read(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember,
                    progression);
                if (!(totalGrantedValue is int))
                {
                    error = "The live progression intelligence-skill-point total is not an Int32 value.";
                    return false;
                }
                int totalGranted = (int)totalGrantedValue;
                // ApplySkillPoints.Apply defines granted = total - pre-level baseline; recover that
                // baseline so a repeated refresh reproduces exactly one full replay of the action.
                int preLevelBaseline = totalGranted - currentGranted;
                previous = new DerivedAllowanceSnapshot(currentGranted, totalGranted);

                object recomputed = contracts.LevelUpHelperGetTotalIntelligenceSkillPointsMethod.Invoke(
                    null,
                    new[] { unit, nextLevel });
                if (!(recomputed is int))
                {
                    error = "LevelUpHelper.GetTotalIntelligenceSkillPoints did not return an Int32 value.";
                    return false;
                }
                int total = (int)recomputed;
                int granted = total - preLevelBaseline;

                ReflectionAccess.Write(
                    contracts.LevelUpStateIntelligenceSkillPointsMember,
                    state,
                    granted);
                ReflectionAccess.Write(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember,
                    progression,
                    total);
                contracts.LevelUpStateOnApplyActionMethod.Invoke(state, null);

                object verifyGranted = ReflectionAccess.Read(
                    contracts.LevelUpStateIntelligenceSkillPointsMember,
                    state);
                object verifyTotal = ReflectionAccess.Read(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember,
                    progression);
                if (!(verifyGranted is int) || (int)verifyGranted != granted ||
                    !(verifyTotal is int) || (int)verifyTotal != total)
                {
                    error = "The refreshed derived allowances did not verify on the live LevelUpState.";
                    return false;
                }

                grantedIntelligenceSkillPoints = granted;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "The native derived-state refresh failed with " +
                    exception.GetType().Name + ": " + exception.Message;
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
            if (previous == null || state == null || unit == null || contracts == null)
            {
                error = "The derived-allowance rollback requires a snapshot and the live objects.";
                return false;
            }
            try
            {
                object progression = ReflectionAccess.Read(contracts.UnitDescriptorProgressionMember, unit);
                if (progression == null)
                {
                    error = "The live preview progression is unavailable for the derived-allowance rollback.";
                    return false;
                }
                ReflectionAccess.Write(
                    contracts.LevelUpStateIntelligenceSkillPointsMember,
                    state,
                    previous.IntelligenceSkillPoints);
                ReflectionAccess.Write(
                    contracts.UnitProgressionTotalIntelligenceSkillPointsMember,
                    progression,
                    previous.ProgressionTotalIntelligenceSkillPoints);
                contracts.LevelUpStateOnApplyActionMethod.Invoke(state, null);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "The derived-allowance rollback failed with " +
                    exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }
    }
}
