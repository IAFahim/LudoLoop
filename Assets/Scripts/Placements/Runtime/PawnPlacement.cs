using System;
using System.Collections.Generic;
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

    [Serializable]
    public struct PawnData
    {
        public GameObject prefab;
    }

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

        public static void SpawnPawns(
            in PlacementConfig config,
            Vector3 origin,
            GameObject prefab,
            List<GameObject> outPawns)
        {
            outPawns.Clear();
            if (config.count <= 0 || prefab == null) return;

            float angleGap = 360f / config.count;
            for (int i = 0; i < config.count; i++)
            {
                float angle = config.startAngle + angleGap * i;
                Vector3 pos = CalculatePosition(config.radius, angle, config.height, origin);
                outPawns.Add(UnityEngine.Object.Instantiate(prefab, pos, config.rotation));
            }
        }
    }

    // Minimal MonoBehaviour wrapper
    public class PawnPlacement : MonoBehaviour
    {
        [SerializeField] private PawnData pawnData;
        [SerializeField] private PlacementConfig config = PlacementConfig.Default();
        [SerializeField] private List<GameObject> spawnedPawns = new();

        private void OnEnable()
        {
            CircularPlacement.SpawnPawns(config, transform.position, pawnData.prefab, spawnedPawns);
        }

        private void OnDrawGizmos()
        {
            if (config.count <= 0) return;

            Vector3 center = transform.position;
            Vector3 heightOffset = Vector3.up * config.height;
            float angleGap = 360f / config.count;

            // Circle
            Gizmos.color = Color.cyan;
            DrawCircle(center + heightOffset, config.radius, 64);

            // Positions
            for (int i = 0; i < config.count; i++)
            {
                float angle = config.startAngle + angleGap * i;
                Vector3 pos = CircularPlacement.CalculatePosition(config.radius, angle, config.height, center);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(pos, 0.05f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(center + heightOffset, pos);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(pos, config.rotation * Vector3.forward * 0.1f);
            }

            // Center
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(center + heightOffset, 0.05f);
        }

        private static void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(
                    radius * Mathf.Cos(angle),
                    0,
                    radius * Mathf.Sin(angle)
                );

                if (i > 0) Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}