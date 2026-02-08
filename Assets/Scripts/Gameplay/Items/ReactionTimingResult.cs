namespace Gameplay.Items
{
    public enum ReactionTimingResult : byte
    {
        None = 0,
        TooEarly,
        Optimal,
        Acceptable,
        TooLate
    }
}
