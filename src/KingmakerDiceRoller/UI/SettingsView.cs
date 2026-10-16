using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Integration;
using UnityEngine;

namespace KingmakerDiceRoller.UI
{
    public sealed class SettingsView
    {
        private static readonly Color NoticeColor = new Color(1f, 0.82f, 0.35f);
        private static readonly Color ProblemColor = new Color(1f, 0.5f, 0.45f);
        private readonly SettingsPanel panel;
        private SettingsPanelModel model;
        private GUIStyle wrappedLabel;

        public SettingsView(
            Settings settings,
            CharacterCreationCoordinator coordinator,
            RuntimeDiagnostics diagnostics,
            KingmakerContractHolder contracts)
        {
            panel = new SettingsPanel(
                coordinator,
                diagnostics,
                () => contracts.Current,
                () => settings.VerboseDiagnostics,
                value => settings.VerboseDiagnostics = value);
        }

        public void SetCompatibility(CompatibilitySnapshot value)
        {
            panel.SetCompatibility(value);
        }

        public void Draw()
        {
            // The model is rebuilt only on Layout so every pass of one IMGUI event draws the same
            // controls; clicks take effect from the next Layout.
            Event current = Event.current;
            if (model == null || current == null || current.type == EventType.Layout) model = panel.Build();
            if (wrappedLabel == null) wrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };

            GUILayout.Label(model.Explanation, wrappedLabel);
            if (model.Status != null) DrawLine(model.Status);
            for (int index = 0; index < model.Notices.Count; index++) DrawLine(model.Notices[index].Line);
            if (model.Recovery != null) DrawRecovery(model.Recovery);

            GUILayout.Space(6f);
            if (GUILayout.Button(model.TroubleshootingButtonLabel, GUILayout.ExpandWidth(false))) panel.ToggleTroubleshooting();
            if (model.Troubleshooting != null) DrawTroubleshooting(model.Troubleshooting);
        }

        private void DrawRecovery(RecoveryAction recovery)
        {
            GUILayout.Space(6f);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && recovery.Enabled;
            if (GUILayout.Button(RecoveryAction.Label, GUILayout.ExpandWidth(false))) panel.ReturnToPointBuy();
            GUI.enabled = oldEnabled;
            DrawLine(recovery.Explanation);
        }

        private void DrawTroubleshooting(TroubleshootingSection troubleshooting)
        {
            bool verbose = GUILayout.Toggle(troubleshooting.VerboseDiagnostics, TroubleshootingSection.VerboseDiagnosticsLabel);
            if (verbose != troubleshooting.VerboseDiagnostics) panel.SetVerboseDiagnostics(verbose);
            for (int index = 0; index < troubleshooting.Details.Count; index++)
            {
                GUILayout.Label(troubleshooting.Details[index], wrappedLabel);
            }
        }

        private void DrawLine(PanelLine line)
        {
            Color oldColor = GUI.contentColor;
            if (line.Tone == PanelTone.Notice) GUI.contentColor = NoticeColor;
            else if (line.Tone == PanelTone.Problem) GUI.contentColor = ProblemColor;
            GUILayout.Label(line.Text, wrappedLabel);
            GUI.contentColor = oldColor;
        }
    }
}
