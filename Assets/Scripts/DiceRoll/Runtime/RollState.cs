using System;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct RollState
    {
        public DiceState state;
        public float settledTimer;
        public float returnTimer;

        public static RollState Default()
        {
            return new RollState
            {
                state = DiceState.InHand,
            };
        }
    }
}