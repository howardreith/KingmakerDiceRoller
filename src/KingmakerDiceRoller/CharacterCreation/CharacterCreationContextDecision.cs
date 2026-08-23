namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationContextDecision
    {
        private CharacterCreationContextDecision(
            bool accepted,
            string reason,
            SupportedCharacterCreationKind? creationKind,
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            bool controllerStateMatches,
            bool controllerUnitMatches,
            bool controllerPreviewMatches,
            MainCharacterIdentityRelation? mainCharacterRelation,
            string mode,
            bool? isFirstLevel,
            bool? candidateIsMainCharacter,
            bool? candidateIsPlayerFaction,
            bool? candidateIsPet,
            bool? candidateIsEnemy,
            MercenaryDiscriminatorEvidence mercenaryEvidence,
            string stableOwnerSource)
        {
            Accepted = accepted;
            Reason = reason;
            CreationKind = creationKind;
            Controller = controller;
            StableOwner = stableOwner;
            State = state;
            Unit = unit;
            Distribution = distribution;
            ControllerStateMatches = controllerStateMatches;
            ControllerUnitMatches = controllerUnitMatches;
            ControllerPreviewMatches = controllerPreviewMatches;
            MainCharacterRelation = mainCharacterRelation;
            Mode = mode ?? string.Empty;
            IsFirstLevel = isFirstLevel;
            CandidateIsMainCharacter = candidateIsMainCharacter;
            CandidateIsPlayerFaction = candidateIsPlayerFaction;
            CandidateIsPet = candidateIsPet;
            CandidateIsEnemy = candidateIsEnemy;
            MercenaryEvidence = mercenaryEvidence ?? new MercenaryDiscriminatorEvidence(false, false);
            StableOwnerSource = stableOwnerSource ?? string.Empty;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public SupportedCharacterCreationKind? CreationKind { get; }
        public object Controller { get; }
        public object StableOwner { get; }
        public object State { get; }
        public object Unit { get; }
        public object Distribution { get; }
        public bool ControllerStateMatches { get; }
        public bool ControllerUnitMatches { get; }
        public bool ControllerPreviewMatches { get; }
        public MainCharacterIdentityRelation? MainCharacterRelation { get; }
        public string Mode { get; }
        public bool? IsFirstLevel { get; }
        public bool? CandidateIsMainCharacter { get; }
        public bool? CandidateIsPlayerFaction { get; }
        public bool? CandidateIsPet { get; }
        public bool? CandidateIsEnemy { get; }
        public MercenaryDiscriminatorEvidence MercenaryEvidence { get; }
        public string StableOwnerSource { get; }
        public object CandidateUnit => Unit;

        public static CharacterCreationContextDecision Accept(
            SupportedCharacterCreationKind creationKind,
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            string reason,
            bool controllerStateMatches,
            bool controllerUnitMatches,
            bool controllerPreviewMatches,
            MainCharacterIdentityRelation mainCharacterRelation,
            string mode,
            bool isFirstLevel,
            bool candidateIsMainCharacter,
            bool candidateIsPlayerFaction,
            bool candidateIsPet,
            bool candidateIsEnemy,
            MercenaryDiscriminatorEvidence mercenaryEvidence,
            string stableOwnerSource)
        {
            return new CharacterCreationContextDecision(
                true,
                reason,
                creationKind,
                controller,
                stableOwner,
                state,
                unit,
                distribution,
                controllerStateMatches,
                controllerUnitMatches,
                controllerPreviewMatches,
                mainCharacterRelation,
                mode,
                isFirstLevel,
                candidateIsMainCharacter,
                candidateIsPlayerFaction,
                candidateIsPet,
                candidateIsEnemy,
                mercenaryEvidence,
                stableOwnerSource);
        }

        public static CharacterCreationContextDecision Reject(string reason)
        {
            return new CharacterCreationContextDecision(
                false,
                reason,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public static CharacterCreationContextDecision Reject(
            string reason,
            MainCharacterIdentityRelation mainCharacterRelation)
        {
            return new CharacterCreationContextDecision(
                false,
                reason,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                mainCharacterRelation,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }
    }
}
