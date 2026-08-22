namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBuyRestoreObservation
    {
        internal PointBuyRestoreObservation(
            LivePreviewObservation livePreview,
            bool rolledDistributionMatches,
            bool rolledUnitMatches,
            bool fullAllocatorBudgetAvailable)
        {
            LivePreview = livePreview;
            RolledDistributionMatches = rolledDistributionMatches;
            RolledUnitMatches = rolledUnitMatches;
            FullAllocatorBudgetAvailable = fullAllocatorBudgetAvailable;
        }

        public LivePreviewObservation LivePreview { get; }
        public bool RolledDistributionMatches { get; }
        public bool RolledUnitMatches { get; }
        public bool FullAllocatorBudgetAvailable { get; }
        public bool RolledAssignmentStillPresent => RolledDistributionMatches && RolledUnitMatches;
        public bool HybridStateDetected => RolledAssignmentStillPresent && FullAllocatorBudgetAvailable;
        public bool IsVerified => LivePreview != null && LivePreview.IsVerified && !HybridStateDetected;

        public string BuildFacts(RollSession session, bool refreshInProgress)
        {
            PointBuyOrigin pristine = session.PointBuyOrigin;
            return "Facts: pointBuyOriginCaptured=" + BooleanText(session.PointBuyOriginCaptured) +
                ", pristineBaselineGeneration=" + pristine.CapturedGeneration +
                ", currentGeneration=" + session.Generation +
                ", candidateBaselineContaminated=" + BooleanText(session.CandidateBaselineContaminated) +
                ", mode=" + session.Mode +
                ", allocatorBudget=" + pristine.AllocatorBudget +
                ", liveDistributionMatchesPristine=" + BooleanText(LivePreview != null && LivePreview.LiveDistributionValuesMatch) +
                ", liveUnitMatchesPristine=" + BooleanText(LivePreview != null && LivePreview.LiveUnitValuesMatch) +
                ", liveAllocatorMatchesPristine=" + BooleanText(LivePreview != null && LivePreview.LiveAllocatorStateMatches) +
                ", rolledAssignmentStillPresent=" + BooleanText(RolledAssignmentStillPresent) +
                ", fullAllocatorBudgetAvailable=" + BooleanText(FullAllocatorBudgetAvailable) +
                ", rollSuppressedForStableOwner=" + BooleanText(session.RollSuppressedForStableOwner) +
                ", refreshInProgress=" + BooleanText(refreshInProgress) + ".";
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
