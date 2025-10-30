using System;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

namespace Placements.Runtime
{
    public class GroupedTiles : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public GameObject tileSafe;
        public GameObject[] tileStart;
        public GameObject[] tileNew;
        public GameObject[] tileBottom;
        public GameObject[] tileFinal;

        public Color[] colors;
        private MaterialPropertyBlock _materialPropertyBlock;

        private void Awake()
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        [Button]
        public void SetColor(int colorIndex)
        {
            Color targetColor = colors[colorIndex];
    
            // Set color for tileSafe
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColor, targetColor);
            tileSafe.GetComponent<Renderer>().SetPropertyBlock(mpb);
    
            // Set color for each tile in tileFinal
            foreach (var o in tileFinal)
            {
                mpb = new MaterialPropertyBlock();
                mpb.SetColor(BaseColor, targetColor);
                o.GetComponent<Renderer>().SetPropertyBlock(mpb);
            }
        }

        public IEnumerable<GameObject> FirstTiles
        {
            get
            {
                yield return tileSafe;
                foreach (var tile in tileStart)
                {
                    yield return tile;
                }
            }
        }

        public IEnumerable<GameObject> EndTiles
        {
            get
            {
                foreach (var tile in tileNew)
                {
                    yield return tile;
                }

                foreach (var tile in tileBottom)
                {
                    yield return tile;
                }
            }
        }
    }
}