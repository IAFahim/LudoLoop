using System.Linq;
using Ludo;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private TokenBase tokenBasePrefab;
    [SerializeField] private HomeBase homeBasePrefab;
    [SerializeField] private Vector3[] pawnBasePositions;
    [SerializeField] private Vector3[] homeBasePositions;
    public TokenBase[] pawnBases { get; private set; }
    public HomeBase[] homes { get; private set; }

    private void OnValidate()
    {
        // Find all TokenBase objects in the scene
        var allTokenBases = FindObjectsByType<TokenBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(pb => pb.name).ToArray();
        pawnBasePositions = allTokenBases.Select(pb => pb.transform.position).ToArray();

        // Find all HomeBase objects
        var allHomeBases = FindObjectsByType<HomeBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(hb => hb.name).ToArray();
        homeBasePositions = allHomeBases.Select(hb => hb.transform.position).ToArray();
    }

    public void SetupPlayers(int playerCount)
    {
        pawnBases = new TokenBase[playerCount];
        homes = new HomeBase[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            var pawnBase = Instantiate(tokenBasePrefab, pawnBasePositions[i % pawnBasePositions.Length], Quaternion.identity);
            pawnBase.name = $"TokenBase_{i}";
            pawnBase.Place(i);
            pawnBases[i] = pawnBase;

            var home = Instantiate(homeBasePrefab, homeBasePositions[i % homeBasePositions.Length], Quaternion.identity);
            home.name = $"HomeBase_{i}";
            homes[i] = home;
        }
    }
}