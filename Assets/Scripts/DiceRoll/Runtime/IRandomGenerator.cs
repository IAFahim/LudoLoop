using UnityEngine;

namespace DiceRoll.Runtime
{
    public interface IRandomGenerator
    {
        void SetSeed(int seed);
        float Range(float min, float max);
        Vector3 InsideUnitSphere();
        Quaternion Rotation();
    }
}