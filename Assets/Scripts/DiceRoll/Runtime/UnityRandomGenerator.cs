using UnityEngine;

namespace DiceRoll.Runtime
{
    public class UnityRandomGenerator : IRandomGenerator
    {
        public void SetSeed(int seed)
        {
            UnityEngine.Random.InitState(seed);
        }

        public float Range(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public Vector3 InsideUnitSphere()
        {
            return UnityEngine.Random.insideUnitSphere.normalized;
        }

        public Quaternion Rotation()
        {
            return UnityEngine.Random.rotation;
        }
    }
}