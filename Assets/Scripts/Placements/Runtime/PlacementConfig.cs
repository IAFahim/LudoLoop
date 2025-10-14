using System;
using UnityEngine;

namespace Placements.Runtime
{
    [Serializable]
    public struct PlacementConfig
    {
        public float radius;
        public float startAngle;
        public float height;
        public Quaternion rotation;
        public int count;

        public static PlacementConfig Default() => new()
        {
            radius = 0.2f,
            startAngle = 45f,
            height = 0.51f,
            rotation = Quaternion.identity,
            count = 4
        };
    }
}