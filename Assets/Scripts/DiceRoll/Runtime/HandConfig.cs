using System;
using UnityEngine;

namespace DiceRoll.Runtime
{
    [Serializable]
    public class HandConfig
    {
        public Vector3 handPosition;
        public Quaternion handRotation;

        public static HandConfig Default()
        {
            return new HandConfig()
            {
                handRotation = Quaternion.identity
            };
        }
    }
}