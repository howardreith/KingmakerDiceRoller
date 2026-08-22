namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollPresentationObservation
    {
        internal RollPresentationObservation(
            bool semanticRollVerified,
            bool refreshRequested,
            int refreshCount,
            bool activeAbilityPhaseFound,
            bool stateMatches,
            bool distributionMatches,
            bool sourceMatches,
            bool previewMatches,
            bool nativeControlsSuppressed,
            int requestedGeneration,
            int currentGeneration,
            string failure)
        {
            SemanticRollVerified = semanticRollVerified;
            RefreshRequested = refreshRequested;
            RefreshCount = refreshCount;
            ActiveAbilityPhaseFound = activeAbilityPhaseFound;
            StateMatches = stateMatches;
            DistributionMatches = distributionMatches;
            SourceMatches = sourceMatches;
            PreviewMatches = previewMatches;
            NativeControlsSuppressed = nativeControlsSuppressed;
            RequestedGeneration = requestedGeneration;
            CurrentGeneration = currentGeneration;
            Failure = failure;
        }

        public bool SemanticRollVerified { get; }
        public bool RefreshRequested { get; }
        public int RefreshCount { get; }
        public bool ActiveAbilityPhaseFound { get; }
        public bool StateMatches { get; }
        public bool DistributionMatches { get; }
        public bool SourceMatches { get; }
        public bool PreviewMatches { get; }
        public bool NativeControlsSuppressed { get; }
        public int RequestedGeneration { get; }
        public int CurrentGeneration { get; }
        public string Failure { get; }
        public bool ViewModelMatches => SourceMatches && PreviewMatches;
        public bool IsSynchronized =>
            SemanticRollVerified && RefreshRequested && RefreshCount == 1 &&
            ActiveAbilityPhaseFound && StateMatches && DistributionMatches &&
            ViewModelMatches && NativeControlsSuppressed &&
            RequestedGeneration == CurrentGeneration;

        public string BuildFacts()
        {
            return "semanticRollVerified=" + Bool(SemanticRollVerified) +
                ", presentationRefreshRequested=" + Bool(RefreshRequested) +
                ", presentationRefreshMethod=" + AbilityPhasePresentationService.NativeRefreshMethod +
                ", presentationRefreshCount=" + RefreshCount +
                ", activeAbilityPhaseFound=" + Bool(ActiveAbilityPhaseFound) +
                ", abilityPhaseStateMatchesSession=" + Bool(StateMatches) +
                ", abilityPhaseDistributionMatchesSession=" + Bool(DistributionMatches) +
                ", abilityPhaseViewModelMatchesSession=" + Bool(ViewModelMatches) +
                ", nativePointBuyControlsSuppressed=" + Bool(NativeControlsSuppressed) +
                ", requestedGeneration=" + RequestedGeneration +
                ", currentGeneration=" + CurrentGeneration + ".";
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
