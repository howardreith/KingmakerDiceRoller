namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class LivePreviewObservation
    {
        internal LivePreviewObservation(
            bool observationSucceeded,
            bool sameStableOwner,
            bool currentControllerStateMatches,
            bool currentControllerPreviewMatches,
            bool currentStateDistributionMatches,
            bool liveDistributionValuesMatch,
            bool liveUnitValuesMatch,
            bool liveAllocatorStateMatches,
            string failure)
        {
            ObservationSucceeded = observationSucceeded;
            SameStableOwner = sameStableOwner;
            CurrentControllerStateMatches = currentControllerStateMatches;
            CurrentControllerPreviewMatches = currentControllerPreviewMatches;
            CurrentStateDistributionMatches = currentStateDistributionMatches;
            LiveDistributionValuesMatch = liveDistributionValuesMatch;
            LiveUnitValuesMatch = liveUnitValuesMatch;
            LiveAllocatorStateMatches = liveAllocatorStateMatches;
            Failure = failure;
        }

        public bool ObservationSucceeded { get; }
        public bool SameStableOwner { get; }
        public bool CurrentControllerStateMatches { get; }
        public bool CurrentControllerPreviewMatches { get; }
        public bool CurrentStateDistributionMatches { get; }
        public bool LiveDistributionValuesMatch { get; }
        public bool LiveUnitValuesMatch { get; }
        public bool LiveAllocatorStateMatches { get; }
        public string Failure { get; }

        public bool HasCurrentLiveBinding =>
            ObservationSucceeded &&
            SameStableOwner &&
            CurrentControllerStateMatches &&
            CurrentControllerPreviewMatches &&
            CurrentStateDistributionMatches;

        public bool IsVerified =>
            HasCurrentLiveBinding &&
            LiveDistributionValuesMatch &&
            LiveUnitValuesMatch &&
            LiveAllocatorStateMatches;

        public string BuildFacts(RollSession session, bool refreshInProgress)
        {
            return "Facts: applicationGeneration=" + session.Generation +
                ", refreshInProgress=" + BooleanText(refreshInProgress) +
                ", pendingReplacementObserved=" + BooleanText(session.PendingReplacementObserved) +
                ", sameStableOwner=" + BooleanText(SameStableOwner) +
                ", reboundPreview=" + BooleanText(session.ReboundPreview) +
                ", currentControllerStateMatches=" + BooleanText(CurrentControllerStateMatches) +
                ", currentControllerPreviewMatches=" + BooleanText(CurrentControllerPreviewMatches) +
                ", currentStateDistributionMatches=" + BooleanText(CurrentStateDistributionMatches) +
                ", liveDistributionMatches=" + BooleanText(LiveDistributionValuesMatch) +
                ", liveUnitValuesMatch=" + BooleanText(LiveUnitValuesMatch) +
                ", liveAllocatorMatches=" + BooleanText(LiveAllocatorStateMatches) + ".";
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
