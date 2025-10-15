using UnityEngine;

namespace Placements.Runtime
{
    public class Token : MonoBehaviour
    {
        public int tokenIndex;
        public Renderer selfRenderer;
        public Color[] playerTeamTokenColors;
        public Material normalMaterial;
        public Material blinkMaterial;

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
            SetColor(index);
        }

        private void SetColor(int index)
        {
            selfRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(Color, playerTeamTokenColors[index]);
            selfRenderer.SetPropertyBlock(_propBlock);
        }

        public void SetBlinkMaterial()
        {
            selfRenderer.material = blinkMaterial;
            SetColor(tokenIndex);
        }
        
        
        public void SetNormalMaterial()
        {
            selfRenderer.material = normalMaterial;
            SetColor(tokenIndex);
        }
    }
}