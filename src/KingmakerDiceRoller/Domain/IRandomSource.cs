namespace KingmakerDiceRoller.Domain
{
    public interface IRandomSource
    {
        int NextInclusive(int minimum, int maximum);
    }
}
