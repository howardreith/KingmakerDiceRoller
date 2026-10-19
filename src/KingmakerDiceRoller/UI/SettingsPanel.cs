using System;
using System.Collections.Generic;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.UI
{
    public enum PanelTone
    {
        Normal,
        Notice,
        Problem
    }

    public enum PanelNoticeKind
    {
        DisableRefused,
        RollNotApplied,
        ReturnFailed,
        ReturnDerivedValuesStale,
        ReturnPageStale,
        FinalizationFailed,
        CompatibilityUnverified
    }

    public sealed class PanelLine
    {
        public PanelLine(PanelTone tone, string text)
        {
            Tone = tone;
            Text = text;
        }

        public PanelTone Tone { get; }
        public string Text { get; }
    }

    public sealed class PanelNotice
    {
        public PanelNotice(PanelNoticeKind kind, PanelTone tone, string text)
        {
            Kind = kind;
            Line = new PanelLine(tone, text);
        }

        public PanelNoticeKind Kind { get; }
        public PanelLine Line { get; }
    }

    public sealed class RecoveryAction
    {
        public const string Label = "Return to Point Buy";

        public RecoveryAction(bool enabled, PanelLine explanation)
        {
            Enabled = enabled;
            Explanation = explanation;
        }

        public bool Enabled { get; }

        // The action's effect while enabled, or the reason it is blocked.
        public PanelLine Explanation { get; }
    }

    public sealed class TroubleshootingSection
    {
        public const string VerboseDiagnosticsLabel = "Verbose diagnostic logging";

        public TroubleshootingSection(bool verboseDiagnostics, IReadOnlyList<string> details)
        {
            VerboseDiagnostics = verboseDiagnostics;
            Details = details;
        }

        public bool VerboseDiagnostics { get; }
        public IReadOnlyList<string> Details { get; }
    }

    public sealed class SettingsPanelModel
    {
        public SettingsPanelModel(
            string explanation,
            PanelLine status,
            IReadOnlyList<PanelNotice> notices,
            RecoveryAction recovery,
            string troubleshootingButtonLabel,
            TroubleshootingSection troubleshooting,
            LowScorePolicy lowScorePolicy,
            int minimumScore)
        {
            Explanation = explanation;
            Status = status;
            Notices = notices;
            Recovery = recovery;
            TroubleshootingButtonLabel = troubleshootingButtonLabel;
            Troubleshooting = troubleshooting;
            LowScorePolicy = lowScorePolicy;
            MinimumScore = minimumScore;
        }

        public string Explanation { get; }
        public PanelLine Status { get; }

        // Always visible, whether or not Troubleshooting is expanded.
        public IReadOnlyList<PanelNotice> Notices { get; }

        // Null unless an active or unresolved roll state makes Return to Point Buy relevant.
        public RecoveryAction Recovery { get; }

        public string TroubleshootingButtonLabel { get; }

        // Null while Troubleshooting is collapsed.
        public TroubleshootingSection Troubleshooting { get; }
        public bool TroubleshootingExpanded => Troubleshooting != null;
        public LowScorePolicy LowScorePolicy { get; }
        public int MinimumScore { get; }
    }

    // Unity-free presentation and interaction state for the UMM panel. Building the model only
    // reads runtime state; SettingsView renders it. The native ability page remains the roll UI.
    public sealed class SettingsPanel
    {
        public const string Explanation =
            "Roll controls appear on the ability-score page when you create a new main character, " +
            "recruit a mercenary, or respec all six starting scores.";
        public const string ShowTroubleshootingLabel = "Show troubleshooting";
        public const string HideTroubleshootingLabel = "Hide troubleshooting";

        private readonly CharacterCreationCoordinator coordinator;
        private readonly RuntimeDiagnostics diagnostics;
        private readonly Func<KingmakerContracts> contractsProvider;
        private readonly Func<bool> verboseDiagnostics;
        private readonly Action<bool> setVerboseDiagnostics;
        private CompatibilitySnapshot compatibility;
        // Presentation only; never persisted.
        private bool troubleshootingExpanded;

        public SettingsPanel(
            CharacterCreationCoordinator coordinator,
            RuntimeDiagnostics diagnostics,
            Func<KingmakerContracts> contractsProvider,
            Func<bool> verboseDiagnostics,
            Action<bool> setVerboseDiagnostics)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            this.contractsProvider = contractsProvider ?? throw new ArgumentNullException(nameof(contractsProvider));
            this.verboseDiagnostics = verboseDiagnostics ?? throw new ArgumentNullException(nameof(verboseDiagnostics));
            this.setVerboseDiagnostics = setVerboseDiagnostics ?? throw new ArgumentNullException(nameof(setVerboseDiagnostics));
        }

        public void SetCompatibility(CompatibilitySnapshot value)
        {
            compatibility = value;
        }

        public void ToggleTroubleshooting()
        {
            troubleshootingExpanded = !troubleshootingExpanded;
        }

        public void SetVerboseDiagnostics(bool value)
        {
            if (verboseDiagnostics() != value) setVerboseDiagnostics(value);
        }

        public void SetLowScorePolicy(LowScorePolicy value)
        {
            coordinator.SetLowScorePolicy(value);
        }

        public void SetMinimumScore(int value)
        {
            coordinator.SetMinimumScore(value);
        }

        public void ReturnToPointBuy()
        {
            // The emergency action's existing eligibility; the coordinator keeps every restore check.
            if (!coordinator.CanRestorePointBuy) return;
            string error;
            if (!coordinator.TryRestorePointBuy(out error)) diagnostics.SetStatus("Point-buy restoration failed: " + error);
        }

        public SettingsPanelModel Build()
        {
            RollSession session = coordinator.ActiveSession;
            KingmakerContracts contracts = contractsProvider();
            RollConfiguration configuration = coordinator.UiSnapshot.Configuration;
            return new SettingsPanelModel(
                Explanation,
                BuildStatus(session, contracts),
                BuildNotices(session),
                BuildRecovery(session),
                troubleshootingExpanded ? HideTroubleshootingLabel : ShowTroubleshootingLabel,
                troubleshootingExpanded ? BuildTroubleshooting(session, contracts) : null,
                configuration.LowScorePolicy,
                configuration.MinimumScore);
        }

        private PanelLine BuildStatus(RollSession session, KingmakerContracts contracts)
        {
            switch (diagnostics.Activation)
            {
                case RuntimeActivation.EnableFailed:
                    return new PanelLine(PanelTone.Problem,
                        "Status: Could not start, so roll controls are unavailable. See Troubleshooting.");
                case RuntimeActivation.NotEnabled:
                case RuntimeActivation.Disabled:
                    return new PanelLine(PanelTone.Normal, "Status: Off.");
            }

            if (contracts == null) return new PanelLine(PanelTone.Problem, "Status: Unavailable. See Troubleshooting.");
            if (session == null) return new PanelLine(PanelTone.Normal, "Status: Ready.");
            if (session.IsPointBuyMode)
            {
                return new PanelLine(PanelTone.Normal, "Status: " + Capitalize(Subject(session)) + " can use Roll Stats.");
            }
            if (session.IsApplied)
            {
                return new PanelLine(PanelTone.Normal, "Status: " + Capitalize(Subject(session)) + " is using rolled scores.");
            }
            return new PanelLine(PanelTone.Normal, "Status: Roll Mode is on for " + Subject(session) + ".");
        }

        private IReadOnlyList<PanelNotice> BuildNotices(RollSession session)
        {
            var notices = new List<PanelNotice>();
            // Disabling is refused only while the session cannot be durably returned to point buy.
            bool disableRefused = diagnostics.Activation == RuntimeActivation.Enabled &&
                diagnostics.DisableRefused && session != null && !session.IsPointBuyMode;
            if (disableRefused)
            {
                notices.Add(new PanelNotice(PanelNoticeKind.DisableRefused, PanelTone.Problem,
                    "The mod could not be turned off because point buy could not be restored. See Troubleshooting."));
            }
            if (session != null && session.IsApplicationFailed)
            {
                notices.Add(new PanelNotice(PanelNoticeKind.RollNotApplied, PanelTone.Problem,
                    "Rolled scores could not be applied to " + Subject(session) + ". See Troubleshooting."));
            }
            switch (coordinator.LastPointBuyReturn)
            {
                case PointBuyReturnOutcome.Failed:
                    // A stuck restore is explained by the blocked recovery action instead.
                    if (!disableRefused && !session.IsRestoringPointBuy)
                    {
                        notices.Add(new PanelNotice(PanelNoticeKind.ReturnFailed, PanelTone.Problem,
                            "Return to Point Buy did not complete. See Troubleshooting."));
                    }
                    break;
                case PointBuyReturnOutcome.DerivedValuesNotRefreshed:
                    notices.Add(new PanelNotice(PanelNoticeKind.ReturnDerivedValuesStale, PanelTone.Problem,
                        "Point buy was restored, but skill points and other derived values did not update. See Troubleshooting."));
                    break;
                case PointBuyReturnOutcome.PageNotRefreshed:
                    notices.Add(new PanelNotice(PanelNoticeKind.ReturnPageStale, PanelTone.Notice,
                        "Point buy was restored, but the ability-score page may still show the rolled scores."));
                    break;
            }
            if (diagnostics.LastFinalizationFailed)
            {
                notices.Add(new PanelNotice(PanelNoticeKind.FinalizationFailed, PanelTone.Problem,
                    "Your last finished character may not have kept its rolled scores. See Troubleshooting."));
            }
            if (compatibility != null && compatibility.Unverified.Count > 0)
            {
                var names = new List<string>();
                for (int index = 0; index < compatibility.Unverified.Count; index++)
                {
                    names.Add(compatibility.Unverified[index].ModName);
                }
                notices.Add(new PanelNotice(PanelNoticeKind.CompatibilityUnverified, PanelTone.Notice,
                    "Compatibility with " + JoinNames(names) + " is unverified. See Troubleshooting."));
            }
            return notices;
        }

        private RecoveryAction BuildRecovery(RollSession session)
        {
            // Point buy needs no recovery; every other mode is an active or interrupted roll state.
            if (session == null || session.IsPointBuyMode) return null;
            if (coordinator.CanRestorePointBuy)
            {
                return new RecoveryAction(true, new PanelLine(PanelTone.Normal,
                    "Restores the point-buy scores you had before rolling."));
            }
            return new RecoveryAction(false, new PanelLine(PanelTone.Problem, session.IsRestoringPointBuy
                ? "Unavailable because an earlier Return to Point Buy did not finish. See Troubleshooting."
                : "Unavailable because a roll did not finish applying. See Troubleshooting."));
        }

        private TroubleshootingSection BuildTroubleshooting(RollSession session, KingmakerContracts contracts)
        {
            bool verbose = verboseDiagnostics();
            RollUiSnapshot workflow = coordinator.UiSnapshot;
            var details = new List<string>
            {
                "Status: " + diagnostics.Status,
                "Activation: " + diagnostics.Activation + (diagnostics.DisableRefused ? "; last disable refused" : string.Empty),
                "Workflow: " + workflow.Mode +
                    "; history: " + workflow.HistoryCount + "/20" +
                    "; saved arrays: " + workflow.SavedCount + "/10",
                session == null
                    ? "Session: none"
                    : "Session: creationKind=" + session.CreationKind +
                        ", mode=" + session.Mode +
                        ", generation=" + session.Generation +
                        ", applied=" + BooleanText(session.IsApplied) +
                        ", applicationFailed=" + BooleanText(session.IsApplicationFailed) +
                        ", liveBinding=" + BooleanText(coordinator.CanAttachNativePanel),
                "Return to Point Buy: " + DescribeRecovery(session)
            };
            if (coordinator.LastPointBuyReturn != PointBuyReturnOutcome.None)
            {
                details.Add("Last Return to Point Buy: " + coordinator.LastPointBuyReturn + "; " + coordinator.LastPointBuyReturnDetail);
            }
            if (diagnostics.LastFinalizationDetail != null)
            {
                details.Add("Last finalization: " + (diagnostics.LastFinalizationFailed ? "FAIL " : "PASS ") +
                    diagnostics.LastFinalizationDetail);
            }
            details.Add("Accepted contexts: " + diagnostics.AcceptedContexts +
                "; rejected: " + diagnostics.RejectedContexts +
                "; applications: " + diagnostics.ArraysApplied +
                "; final pass/fail: " + diagnostics.FinalizationsVerified + "/" + diagnostics.FinalizationFailures +
                "; released: " + diagnostics.SessionsReleased);
            details.Add(contracts == null
                ? "Kingmaker contracts: unavailable"
                : "Kingmaker contracts: resolved; MVID " + contracts.AssemblyMvid.ToString("D"));
            if (compatibility == null)
            {
                details.Add("Compatibility: not checked; detection runs when the mod is enabled.");
            }
            else
            {
                if (compatibility.LoadedMods.Count == 0) details.Add("Detected mods: none of the known compatibility mods.");
                for (int index = 0; index < compatibility.LoadedMods.Count; index++)
                {
                    details.Add("Detected: " + compatibility.LoadedMods[index]);
                }
                for (int index = 0; index < compatibility.Unverified.Count; index++)
                {
                    details.Add("Compatibility (" + compatibility.Unverified[index].ModName + "): " +
                        compatibility.Unverified[index].Detail);
                }
            }
            if (verbose)
            {
                string[] recent = diagnostics.SnapshotRecent();
                details.Add("Recent diagnostics (" + recent.Length + "):");
                details.AddRange(recent);
            }
            else
            {
                details.Add("Recent diagnostics: hidden while verbose diagnostic logging is off.");
            }
            return new TroubleshootingSection(verbose, details);
        }

        private string DescribeRecovery(RollSession session)
        {
            RecoveryAction recovery = BuildRecovery(session);
            if (recovery == null) return "not shown; it appears above only while rolled scores are in use.";
            return (recovery.Enabled ? "available. " : "blocked. ") + recovery.Explanation.Text;
        }

        private static string Subject(RollSession session)
        {
            switch (session.CreationKind)
            {
                case SupportedCharacterCreationKind.Mercenary: return "this mercenary";
                case SupportedCharacterCreationKind.Respec: return "this respec";
                default: return "this new character";
            }
        }

        private static string Capitalize(string text)
        {
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static string JoinNames(List<string> names)
        {
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.GetRange(0, names.Count - 1).ToArray()) + " and " + names[names.Count - 1];
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
