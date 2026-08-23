namespace KingmakerDiceRoller.CharacterCreation
{
    public enum SupportedCharacterCreationKind
    {
        NewMainCharacter,
        Mercenary
    }

    public sealed class MercenaryDiscriminatorEvidence
    {
        public const string ExactSource =
            "LevelUpState.IsEmployee + UnitHelper.IsCustomCompanion(LevelUpController.Unit)";

        public MercenaryDiscriminatorEvidence(
            bool stateIsEmployee,
            bool stableOwnerIsCustomCompanion)
        {
            StateIsEmployee = stateIsEmployee;
            StableOwnerIsCustomCompanion = stableOwnerIsCustomCompanion;
        }

        public bool StateIsEmployee { get; }
        public bool StableOwnerIsCustomCompanion { get; }
        public bool IsExactMatch => StateIsEmployee && StableOwnerIsCustomCompanion;
        public string Source => IsExactMatch ? ExactSource : "none";

        public string BuildFacts()
        {
            return "mercenaryStateEmployee=" + BooleanText(StateIsEmployee) +
                ", mercenaryStableOwnerCustom=" + BooleanText(StableOwnerIsCustomCompanion) +
                ", mercenaryDiscriminator=" + Source;
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
