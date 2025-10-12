using System;
using UnityEngine;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct RandomRollConfig
    {
        public Vector2 rollForceLimit;
        public Vector2 torqueForceLimit;
        public Vector2 upwardForceLimit;
        public Vector3 throwDirection;

        public static RandomRollConfig Default() => new()
        {
            rollForceLimit = new Vector2(10f, 15f),
            torqueForceLimit = new Vector2(1f, 3f),
            upwardForceLimit = new Vector2(3f, 5f),
            throwDirection = new Vector3(0, 0, 1)
        };
    }
}