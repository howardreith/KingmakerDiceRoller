namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationContextDecision
    {
        private CharacterCreationContextDecision(
            bool accepted,
            string reason,
            object state,
            object unit,
            object distribution,
            MainCharacterIdentityRelation? mainCharacterRelation)
        {
            Accepted = accepted;
            Reason = reason;
            State = state;
            Unit = unit;
            Distribution = distribution;
            MainCharacterRelation = mainCharacterRelation;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public object State { get; }
        public object Unit { get; }
        public object Distribution { get; }
        public MainCharacterIdentityRelation? MainCharacterRelation { get; }

        public static CharacterCreationContextDecision Accept(
            object state,
            object unit,
            object distribution,
            string reason,
            MainCharacterIdentityRelation mainCharacterRelation)
        {
            return new CharacterCreationContextDecision(
                true,
                reason,
                state,
                unit,
                distribution,
                mainCharacterRelation);
        }

        public static CharacterCreationContextDecision Reject(string reason)
        {
            return new CharacterCreationContextDecision(false, reason, null, null, null, null);
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
                mainCharacterRelation);
        }
    }
}
