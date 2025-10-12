namespace DiceRoll.Runtime
{
    public enum DiceState : byte
    {
        InHand = 0,
        Throwing = 1,
        Rolling = 2,
        Settled = 3,
        ReturningToHand = 4
    }
}