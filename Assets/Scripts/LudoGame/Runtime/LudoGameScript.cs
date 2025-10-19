using UnityEngine;

namespace LudoGame.Runtime
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "MENUNAME", order = 0)]
    public class LudoGameScript : ScriptableObject
    { 
        public LudoGameState GameState;
    }
}