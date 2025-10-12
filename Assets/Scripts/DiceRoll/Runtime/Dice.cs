using System;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct Dice
    {
        public DiceSides currentSide;

        public static Dice Default()
        {
            return new Dice
            {
                currentSide = DiceSides.Top
            };
        }
    }
}