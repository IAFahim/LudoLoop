using System.Collections.Generic;
using UnityEngine;

namespace Placements.Runtime
{
    public static class CircularPlacement
    {
        public static Vector3 CalculatePosition(
            float radius,
            float angleDegrees,
            float height,
            Vector3 origin)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(rad);
            float z = radius * Mathf.Sin(rad);
            return origin + new Vector3(x, height, z);
        }

        public static void GetPositions(
            in PlacementConfig config,
            Vector3 origin,
            List<Vector3> outPositions)
        {
            outPositions.Clear();
            if (config.count <= 0) return;

            float angleGap = 360f / config.count;
            for (int i = 0; i < config.count; i++)
            {
                float angle = config.startAngle + angleGap * i;
                outPositions.Add(CalculatePosition(config.radius, angle, config.height, origin));
            }
        }

        public static Vector3[] SpawnPawns(
            in PlacementConfig config,
            Vector3 origin
            )
        {
            var positions = new Vector3[4];
            float angleGap = 360f / config.count;
            for (int i = 0; i < config.count; i++)
            {
                float angle = config.startAngle + angleGap * i;
                Vector3 pos = CalculatePosition(config.radius, angle, config.height, origin);
                positions[i] = pos;
            }

            return positions;
        }
    }
}