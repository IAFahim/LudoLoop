using System;
using UnityEngine;
using UnityEngine.Events;

namespace Placements.Runtime
{
    public class LudoToken : MonoBehaviour
    {
        public int tokenIndex;
        public Renderer selfRenderer;
        public Color[] playerTeamTokenColors;
        public Material normalMaterial;
        public Material blinkMaterial;

        private MaterialPropertyBlock _propBlock;
        private static readonly int Color = Shader.PropertyToID("_BaseColor");

        public UnityEvent<int> onTokenClicked;

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
        }

        public void SetColor(int index)
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

        private void OnDisable()
        {
            onTokenClicked.RemoveAllListeners();
        }

        public void SetHighlight(bool active)
        {
            
        }
    }
}