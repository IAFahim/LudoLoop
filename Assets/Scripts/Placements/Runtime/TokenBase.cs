using System;
using Placements.Runtime;
using UnityEngine;
using EasyButtons;

public class TokenBase : MonoBehaviour
{
    [SerializeField]
    private TokenData tokenData;
    public Vector3[] tokenBasePositions;

    [SerializeField] private PlacementConfig config = PlacementConfig.Default();
    [SerializeField] private LudoToken[] tokens;
    [SerializeField] private int tokenId;
    

    public LudoToken[] Tokens => tokens;

    private void Start()
    {
        foreach (var ludoToken in tokens)
        {
            ludoToken.SetColor(tokenId);
        }
    }

    [Button]
    private void GetPosition()
    {
        tokenBasePositions = CircularPlacement.SpawnPawns(config, transform.position);
    }


    [Button]
    public void Place()
    {
        tokenBasePositions = CircularPlacement.SpawnPawns(config, transform.position);
        tokens = new LudoToken[4];
        for (var i = 0; i < tokenBasePositions.Length; i++)
        {
            var tokenPos = tokenBasePositions[i];
            var token = Instantiate(tokenData.prefab, tokenPos, Quaternion.identity);
            token.transform.parent = transform;
            var ludoToken = token.GetComponent<LudoToken>();
            ludoToken.SetTokenIndex(tokenId);
            tokens[i] = ludoToken;
        }
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