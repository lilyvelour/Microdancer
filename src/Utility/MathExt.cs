using System;
using System.Runtime.CompilerServices;

namespace Microdancer
{
    public static class MathExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t)
        {
            return ((1.0f - t) * a) + (b * Math.Clamp(t, 0, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpUnclamped(float a, float b, float t)
        {
            return ((1.0f - t) * a) + (b * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvLerp(float a, float b, float v)
        {
            return (v - a) / (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvLerpUnclamped(float a, float b, float v)
        {
            return Math.Clamp((v - a) / (b - a), 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvLerp(DateTime dtA, DateTime dtB, DateTime dtV)
        {
            var numerator = (float)(dtV - dtA).TotalMilliseconds;
            var denominator = (float)(dtB - dtA).TotalMilliseconds;
            return Math.Clamp(numerator / denominator, 0, 1);
        }
    }
}
