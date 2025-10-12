using System;
using UnityEngine;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct RollForceData
    {
        public Vector3 force;
        public Vector3 torque;
        public Quaternion rotation;
    }
}