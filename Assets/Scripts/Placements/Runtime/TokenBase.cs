using UnityEngine;
using UnityEngine.Serialization;

namespace Placements.Runtime
{
    // Minimal MonoBehaviour wrapper
    public class TokenBase : MonoBehaviour
    {
        [FormerlySerializedAs("pawnData")] [SerializeField] private TokenData tokenData;
        [SerializeField] private PlacementConfig config = PlacementConfig.Default();

        [SerializeField] private Token[] tokens;
        [SerializeField] private Vector3[] tokenBasePositions;

        public Token[] Tokens => tokens;

        public Token[] Place(int teamId)
        {
            tokenBasePositions = CircularPlacement.SpawnPawns(config, transform.position, tokenData.prefab, out GameObject[] spawnedPawns);
            tokens = new Token[4];
            for (var i = 0; i < spawnedPawns.Length; i++)
            {
                var tokenColor = spawnedPawns[i].GetComponent<Token>();
                tokenColor.SetTokenIndex(teamId);
                tokens[i] = tokenColor;
            }

            return tokens;
        }

        public void MoveTokenToBase(int index)
        {
            tokens[index].transform.position = tokenBasePositions[index];
        }

        public void OnPawnStart(int tokenIndex)
        {
        }

        public void OnPawnMove(int position)
        {
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