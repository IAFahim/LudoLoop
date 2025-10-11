using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Placements.Runtime
{
    public class PawnPlacement : MonoBehaviour
    {
        [FormerlySerializedAs("pawn")] public PawnComponent pawnComponent;
        public float radius = .2f;
        public float startAngle = 45f;
        public float height = .51f;
        public Quaternion rotation;
        public int spawnCount;

        public List<GameObject> pawns;

        private void OnEnable()
        {
            float angleGap = 360f / spawnCount;
            Transform t = transform;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 position = PlacementImpl.CalculatePositionInXZPlane(
                    radius, startAngle + angleGap * i, height, t
                );

                var pawn = Instantiate(pawnComponent.prefab, position, rotation);
                pawns.Add(pawn);
            }
        }


        private void OnDrawGizmos()
        {
            if (spawnCount <= 0) return;

            Transform t = transform;
            Vector3 center = t.position;
            float angleGap = 360f / spawnCount;

            // Draw circle at height
            Gizmos.color = Color.cyan;
            DrawCircle(center + Vector3.up * height, radius, 64);

            // Draw positions and connections
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = startAngle + angleGap * i;
                Vector3 position = PlacementImpl.CalculatePositionInXZPlane(
                    radius,
                    angle,
                    height,
                    t
                );

                // Draw sphere at position
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(position, 0.05f);

                // Draw line from center to position
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(center + Vector3.up * height, position);

                // Draw direction indicator
                Gizmos.color = Color.red;
                Vector3 forward = rotation * Vector3.forward * 0.1f;
                Gizmos.DrawRay(position, forward);
            }

            // Draw center marker
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(center + Vector3.up * height, 0.05f);
        }

        private void DrawCircle(Vector3 center, float rad, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = rad * Mathf.Cos(angle);
                float z = rad * Mathf.Sin(angle);
                Vector3 point = center + new Vector3(x, 0, z);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, point);
                }

                prevPoint = point;
            }
        }
    }
}

[Serializable]
public struct PawnComponent
{
    public GameObject prefab;
}

public static class PlacementImpl
{
    /// <summary>
    /// Calculates the position for placement on a circular path (XZ-plane).
    /// </summary>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="angle">The angle in degrees (0 = right, 90 = forward).</param>
    /// <param name="height">The spawn height from plane.</param>
    /// <param name="parent">Parent transform for the position calculation.</param>
    /// <returns>The calculated world position.</returns>
    public static Vector3 CalculatePositionInXZPlane(
        float radius,
        float angle,
        float height,
        Transform parent
    )
    {
        float rad = angle * Mathf.Deg2Rad;
        float x = radius * Mathf.Cos(rad);
        float y = height;
        float z = radius * Mathf.Sin(rad);
        Vector3 offset = new Vector3(x, y, z);
        Vector3 origin = parent.position;
        return origin + offset;
    }
}
