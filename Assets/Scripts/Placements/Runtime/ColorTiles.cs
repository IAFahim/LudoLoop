using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Placements.Runtime
{
    public class ColorTiles : MonoBehaviour
    {
        public GameObject tileSafe;
        public GameObject[] tileStart;
        public GameObject[] tileNew;
        public GameObject[] tileBottom;
        public GameObject[] tileFinal;

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
        
        public IEnumerator<GameObject> FinalTiles
        {
            get
            {
                foreach (var tile in tileFinal)
                {
                    yield return tile;
                }
            }
        }
    }
}