namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBuyPresentationObservation
    {
        internal PointBuyPresentationObservation(
            bool semanticPointBuyVerified,
            bool presentationRefreshRequested,
            string presentationRefreshMethod,
            int presentationRefreshCount,
            bool activeAbilityPhaseFound,
            bool abilityPhaseStateMatchesSession,
            bool abilityPhaseDistributionMatchesSession,
            bool abilityPhaseSourceMatchesSession,
            bool abilityPhasePreviewMatchesSession,
            int refreshGeneration,
            int postRefreshGeneration,
            bool postRefreshLiveModelVerified,
            string failure)
        {
            SemanticPointBuyVerified = semanticPointBuyVerified;
            PresentationRefreshRequested = presentationRefreshRequested;
            PresentationRefreshMethod = presentationRefreshMethod;
            PresentationRefreshCount = presentationRefreshCount;
            ActiveAbilityPhaseFound = activeAbilityPhaseFound;
            AbilityPhaseStateMatchesSession = abilityPhaseStateMatchesSession;
            AbilityPhaseDistributionMatchesSession = abilityPhaseDistributionMatchesSession;
            AbilityPhaseSourceMatchesSession = abilityPhaseSourceMatchesSession;
            AbilityPhasePreviewMatchesSession = abilityPhasePreviewMatchesSession;
            RefreshGeneration = refreshGeneration;
            PostRefreshGeneration = postRefreshGeneration;
            PostRefreshLiveModelVerified = postRefreshLiveModelVerified;
            Failure = failure;
        }

        public bool SemanticPointBuyVerified { get; }
        public bool PresentationRefreshRequested { get; }
        public string PresentationRefreshMethod { get; }
        public int PresentationRefreshCount { get; }
        public bool ActiveAbilityPhaseFound { get; }
        public bool AbilityPhaseStateMatchesSession { get; }
        public bool AbilityPhaseDistributionMatchesSession { get; }
        public bool AbilityPhaseSourceMatchesSession { get; }
        public bool AbilityPhasePreviewMatchesSession { get; }
        public bool AbilityPhaseViewModelMatchesSession =>
            AbilityPhaseSourceMatchesSession && AbilityPhasePreviewMatchesSession;
        public int RefreshGeneration { get; }
        public int PostRefreshGeneration { get; }
        public bool PostRefreshLiveModelVerified { get; }
        public string Failure { get; }

        public bool IsSynchronized =>
            SemanticPointBuyVerified &&
            PresentationRefreshRequested &&
            PresentationRefreshCount == 1 &&
            ActiveAbilityPhaseFound &&
            AbilityPhaseStateMatchesSession &&
            AbilityPhaseDistributionMatchesSession &&
            AbilityPhaseViewModelMatchesSession &&
            RefreshGeneration == PostRefreshGeneration &&
            PostRefreshLiveModelVerified;

        public string BuildFacts(RollSession session)
        {
            return "Facts: semanticPointBuyVerified=" + BooleanText(SemanticPointBuyVerified) +
                ", presentationRefreshRequested=" + BooleanText(PresentationRefreshRequested) +
                ", presentationRefreshMethod=" + (PresentationRefreshMethod ?? "unavailable") +
                ", presentationRefreshCount=" + PresentationRefreshCount +
                ", activeAbilityPhaseFound=" + BooleanText(ActiveAbilityPhaseFound) +
                ", abilityPhaseStateMatchesSession=" + BooleanText(AbilityPhaseStateMatchesSession) +
                ", abilityPhaseDistributionMatchesSession=" + BooleanText(AbilityPhaseDistributionMatchesSession) +
                ", abilityPhaseViewModelMatchesSession=" + BooleanText(AbilityPhaseViewModelMatchesSession) +
                ", abilityPhaseSourceMatchesSession=" + BooleanText(AbilityPhaseSourceMatchesSession) +
                ", abilityPhasePreviewMatchesSession=" + BooleanText(AbilityPhasePreviewMatchesSession) +
                ", refreshGeneration=" + RefreshGeneration +
                ", postRefreshGeneration=" + PostRefreshGeneration +
                ", postRefreshLiveModelVerified=" + BooleanText(PostRefreshLiveModelVerified) +
                ", mode=" + session.Mode +
                ", rollSuppressedForStableOwner=" + BooleanText(session.RollSuppressedForStableOwner) + ".";
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
