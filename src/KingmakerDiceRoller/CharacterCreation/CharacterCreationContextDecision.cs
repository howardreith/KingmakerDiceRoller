namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationContextDecision
    {
        private CharacterCreationContextDecision(bool accepted, string reason, object state, object unit, object distribution)
        {
            Accepted = accepted;
            Reason = reason;
            State = state;
            Unit = unit;
            Distribution = distribution;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public object State { get; }
        public object Unit { get; }
        public object Distribution { get; }

        public static CharacterCreationContextDecision Accept(object state, object unit, object distribution)
        {
            return new CharacterCreationContextDecision(true, "Exact new-main-character context.", state, unit, distribution);
        }

        public static CharacterCreationContextDecision Reject(string reason)
        {
            return new CharacterCreationContextDecision(false, reason, null, null, null);
        }
    }
}
