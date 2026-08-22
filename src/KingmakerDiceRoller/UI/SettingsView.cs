using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Integration;
using UnityEngine;

namespace KingmakerDiceRoller.UI
{
    public sealed class SettingsView
    {
        private readonly Settings settings;
        private readonly CharacterCreationCoordinator coordinator;
        private readonly RuntimeDiagnostics diagnostics;
        private readonly KingmakerContractHolder contracts;
        private CompatibilitySnapshot compatibility;

        public SettingsView(
            Settings settings,
            CharacterCreationCoordinator coordinator,
            RuntimeDiagnostics diagnostics,
            KingmakerContractHolder contracts)
        {
            this.settings = settings;
            this.coordinator = coordinator;
            this.diagnostics = diagnostics;
            this.contracts = contracts;
        }

        public void SetCompatibility(CompatibilitySnapshot value)
        {
            compatibility = value;
        }

        public void Draw()
        {
            RollUiSnapshot workflow = coordinator.UiSnapshot;
            GUILayout.Label("Kingmaker Dice Roller " + ProductMetadata.Version);
            GUILayout.Label("Roll controls are on the native new-character ability-score page.");
            settings.VerboseDiagnostics = GUILayout.Toggle(settings.VerboseDiagnostics, "Verbose diagnostic logging");
            GUILayout.Space(6f);
            GUILayout.Label("Workflow: " + workflow.Mode +
                "; history: " + workflow.HistoryCount + "/20" +
                "; saved arrays: " + workflow.SavedCount + "/10");
            GUILayout.Label("Status: " + diagnostics.Status);
            GUILayout.Label("Accepted contexts: " + diagnostics.AcceptedContexts + "; rejected: " + diagnostics.RejectedContexts + "; applications: " + diagnostics.ArraysApplied + "; released: " + diagnostics.SessionsReleased);
            GUILayout.Label(contracts.Current == null
                ? "Kingmaker contracts: unavailable"
                : "Kingmaker contracts: resolved; MVID " + contracts.Current.AssemblyMvid.ToString("D"));

            if (compatibility != null)
            {
                for (int index = 0; index < compatibility.LoadedMods.Count; index++) GUILayout.Label("Detected: " + compatibility.LoadedMods[index]);
                for (int index = 0; index < compatibility.Warnings.Count; index++) GUILayout.Label("WARNING: " + compatibility.Warnings[index]);
            }

            GUILayout.Space(6f);
            GUI.enabled = coordinator.CanRestorePointBuy;
            if (GUILayout.Button("Emergency: Return active Roll Mode to Point Buy"))
            {
                string error;
                if (!coordinator.TryRestorePointBuy(out error)) diagnostics.SetStatus("Point-buy restoration failed: " + error);
            }
            GUI.enabled = true;

            if (settings.VerboseDiagnostics)
            {
                string[] recent = diagnostics.SnapshotRecent();
                for (int index = 0; index < recent.Length; index++) GUILayout.Label(recent[index]);
            }
        }
    }
}
