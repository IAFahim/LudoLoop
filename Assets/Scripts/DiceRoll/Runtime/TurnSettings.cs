using System;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct TurnSettings
    {
        public float velocityThreshold;
        public float angularVelocityThreshold;
        public float settledTime;
        public float returnDelay;
        public float returnSpeed;

        public static TurnSettings Default() => new()
        {
            velocityThreshold = 0.1f,
            angularVelocityThreshold = 0.1f,
            settledTime = 1f,
            returnDelay = 2,
            returnSpeed = 10,
        };
    }
}