using System;
using System.Runtime.CompilerServices;

namespace Spawner.Spawner.Authoring
{
    [Serializable]
    public struct Delay
    {
        public float elapsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public bool IsOver(float duration) => elapsed > duration;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public void Update(float deltaTime) => elapsed += deltaTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public void Reset() => elapsed = 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public float GetRemainingTime(float duration) => duration - elapsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public float GetProgress(float duration) => duration > 0 ? elapsed / duration : 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public float GetOvershoot(float duration) => elapsed > duration ? elapsed - duration : 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public bool UpdateAndCheck(float deltaTime, float duration)
        {
            elapsed += deltaTime;
            return IsOver(duration);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public bool UpdateAndReset(float deltaTime, float duration)
        {
            elapsed += deltaTime;
            if (!IsOver(duration)) return false;
            elapsed -= duration;
            return true;
        }
    }
}