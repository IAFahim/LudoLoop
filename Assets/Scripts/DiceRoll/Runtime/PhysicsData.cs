using System;
using UnityEngine;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct PhysicsData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }
}