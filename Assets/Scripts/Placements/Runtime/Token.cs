using UnityEngine;

namespace Placements.Runtime
{
    public class Token : MonoBehaviour
    {
        public int tokenIndex;
        public Color[] playerTeamTokenColors;
        public Renderer selfRenderer;

        private MaterialPropertyBlock _propBlock;
        private static readonly int Color = Shader.PropertyToID("_Color");

        private void OnValidate()
        {
            selfRenderer = GetComponent<Renderer>();
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        public void SetTokenIndex(int index)
        {
            tokenIndex = index;
            selfRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(Color, playerTeamTokenColors[index]);
            selfRenderer.SetPropertyBlock(_propBlock);
        }
    }
}