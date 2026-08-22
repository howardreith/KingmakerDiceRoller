namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationContextDecision
    {
        private CharacterCreationContextDecision(
            bool accepted,
            string reason,
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            bool controllerStateMatches,
            bool controllerUnitMatches,
            bool controllerPreviewMatches,
            MainCharacterIdentityRelation? mainCharacterRelation)
        {
            Accepted = accepted;
            Reason = reason;
            Controller = controller;
            StableOwner = stableOwner;
            State = state;
            Unit = unit;
            Distribution = distribution;
            ControllerStateMatches = controllerStateMatches;
            ControllerUnitMatches = controllerUnitMatches;
            ControllerPreviewMatches = controllerPreviewMatches;
            MainCharacterRelation = mainCharacterRelation;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public object Controller { get; }
        public object StableOwner { get; }
        public object State { get; }
        public object Unit { get; }
        public object Distribution { get; }
        public bool ControllerStateMatches { get; }
        public bool ControllerUnitMatches { get; }
        public bool ControllerPreviewMatches { get; }
        public MainCharacterIdentityRelation? MainCharacterRelation { get; }

        public static CharacterCreationContextDecision Accept(
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            string reason,
            bool controllerStateMatches,
            bool controllerUnitMatches,
            bool controllerPreviewMatches,
            MainCharacterIdentityRelation mainCharacterRelation)
        {
            return new CharacterCreationContextDecision(
                true,
                reason,
                controller,
                stableOwner,
                state,
                unit,
                distribution,
                controllerStateMatches,
                controllerUnitMatches,
                controllerPreviewMatches,
                mainCharacterRelation);
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
                false,
                false,
                false,
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
                false,
                false,
                false,
                mainCharacterRelation);
        }
    }
}
