using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Patches;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class NativeRespecEntryService
    {
        private readonly NativeRespecContracts contracts;
        private object selectedEntity;
        private Func<bool> selectionInProgress;
        private readonly CharacterCreationCoordinator coordinator;
        public NativeRespecEntryService(NativeRespecContracts contracts, CharacterCreationCoordinator coordinator)
        { this.contracts = contracts; this.coordinator = coordinator; }
        public void BeginSelection(object selector)
        {
            NativeInvocationScope scope = NativeInvocationScope.Capture(typeof(KingmakerPatchBridge));
            BeginSelectionScope(selector, scope.IsActive);
        }
        public void BuildStarted(object source, object success, object mode)
        {
            if (mode == null || mode.ToString() != "Respec") return;
            string reason;
            RespecOwnership owner = ObserveLaunch(source, success, mode, out reason);
            if (owner == null) coordinator.ReportRespecExclusion(reason);
            else
            {
                coordinator.ReportRespecContract(PatchOwnerDiagnostics.Describe(contracts.PlayerRespec, contracts.Game.LevelUpStateConstructor,
                    contracts.Game.LevelUpApplyLevelupMethod, contracts.Game.LevelUpCommitMethod));
                coordinator.OnRespecBound(owner);
            }
        }
        public void BeginCommit(object controller)
        {
            NativeInvocationScope scope = NativeInvocationScope.Capture(typeof(KingmakerPatchBridge));
            coordinator.OnRespecCommitStarted(controller, scope.IsActive);
        }
        public void Cancel(object controller) { coordinator.OnBuildCanceled(controller); }

        private void BeginSelectionScope(object selector, Func<bool> inProgress)
        {
            selectedEntity = null; selectionInProgress = null;
            if (selector == null || !Equals(contracts.shown.GetValue(selector), true) || !inProgress()) return;
            selectedEntity = contracts.selected.GetValue(selector, null);
            selectionInProgress = inProgress;
        }
        public void EndSelection() { selectedEntity = null; selectionInProgress = null; }

        private RespecOwnership ObserveLaunch(object source, object success, object mode, out string reason)
        {
            reason = "Respec has no current player-selector invocation.";
            object selectedOriginal = selectedEntity;
            if (selectionInProgress == null || !selectionInProgress() || selectedOriginal == null || mode == null || mode.ToString() != "Respec") return null;
            EndSelection(); // one selected launch; nested pets/controllers cannot claim it
            Action action = success as Action;
            if (action == null || action.GetInvocationList().Length != 1 || action.Method != contracts.copyCallback || action.Target == null)
            { reason = "Unsupported respec completion callback contract."; return null; }
            object target = action.Target;
            object originalEntity = contracts.original.GetValue(target), sourceEntity = contracts.rebuild.GetValue(target), player = contracts.playerField.GetValue(target);
            object controller, liveSource, state, preview;
            if (!ReferenceEquals(originalEntity, selectedOriginal) || ReferenceEquals(originalEntity, sourceEntity) ||
                !ReferenceEquals(contracts.descriptor.GetValue(sourceEntity, null), source) || !IsOwnedPlayerEntity(player, originalEntity) ||
                !contracts.Game.TryGetLevelUpControllerContext(out controller, out liveSource, out state, out preview) ||
                !ReferenceEquals(source, liveSource) || !Equals(contracts.callback.GetValue(controller), action))
            { reason = "Respec selector, original, rebuild source, callback, or live controller ownership does not match."; return null; }
            if (Convert.ToInt32(ReflectionAccess.Read(contracts.level, ReflectionAccess.Read(contracts.progression, source))) != 0 ||
                !Equals(ReflectionAccess.Read(contracts.Game.LevelUpStateIsFirstLevelMember, state), true) ||
                !Equals(ReflectionAccess.Read(contracts.Game.DistributionAvailableMember, ReflectionAccess.Read(contracts.Game.LevelUpStateDistributionMember, state)), true))
            { reason = "Locked starting scores / recruitment-level retraining; ordinary progression is preserved."; return null; }
            string provider = "Native 2.1.7b";
            FieldInfo providerEnabled = null;
            bool wasEnabled = false;
            Assembly eddic = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "EddicKingmakerRespec");
            if (eddic != null)
            {
                FieldInfo enabled = eddic.GetType("EddicKingmakerRespec.Main")?.GetField("Enabled", BindingFlags.Public | BindingFlags.Static);
                if (enabled == null) { reason = "Unsupported Eddic enabled-state contract."; return null; }
                providerEnabled = enabled;
                wasEnabled = Equals(enabled.GetValue(null), true);
                if (wasEnabled)
                {
                    if (eddic.ManifestModule.ModuleVersionId != new Guid("be15b46a-80b0-417c-a62d-b3f6aea24c55"))
                    { reason = "Unsupported Eddic assembly contract."; return null; }
                    provider = "EddicKingmakerRespec 1.0";
                }
            }
            reason = "Verified selector -> native RespecCompanion -> first-level rebuild -> original-entity copy callback; " + provider;
            return new RespecOwnership(controller, source, originalEntity, action, provider,
                () => (providerEnabled == null || Equals(providerEnabled.GetValue(null), wasEnabled)) &&
                    IsOwnedPlayerEntity(player, originalEntity) && ReferenceEquals(contracts.original.GetValue(target), originalEntity) &&
                    ReferenceEquals(contracts.rebuild.GetValue(target), sourceEntity) && Equals(contracts.callback.GetValue(controller), action),
                () => contracts.descriptor.GetValue(originalEntity, null));
        }

        private bool IsOwnedPlayerEntity(object player, object entity)
        {
            object gameInstance = ReflectionAccess.Read(contracts.Game.GameInstanceMember, null);
            if (gameInstance == null || !ReferenceEquals(ReflectionAccess.Read(contracts.Game.GamePlayerMember, gameInstance), player)) return false;
            object unit = contracts.descriptor.GetValue(entity, null);
            bool value; string path;
            if (!ReflectionAccess.TryReadBoolean(unit, new[] { "IsPlayerFaction", "Unit.IsPlayerFaction" }, out value, out path) || !value ||
                !ReflectionAccess.TryReadBoolean(unit, new[] { "IsPet", "Unit.IsPet" }, out value, out path) || value ||
                !ReflectionAccess.TryReadBoolean(unit, new[] { "IsPlayersEnemy", "Unit.IsPlayersEnemy" }, out value, out path) || value) return false;
            foreach (PropertyInfo member in new[] { contracts.party, contracts.remote })
                foreach (object reference in (IEnumerable)member.GetValue(player, null))
                    if (ReferenceEquals(contracts.referenceValue.GetValue(reference, null), entity)) return true;
            return false;
        }

    }
}
