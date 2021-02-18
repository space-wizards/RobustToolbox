using System;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Maths
{
    [Obsolete("Use MathHelper instead.")]
    public static class FloatMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float val)
        {
            return MathHelper.Clamp01(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            return MathHelper.Clamp(val, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float val, float min, float max)
        {
            return MathHelper.Clamp(val, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double val, double min, double max)
        {
            return MathHelper.Clamp(val, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseTo(float a, float b, double tolerance = .00001)
        {
            return MathHelper.CloseTo(a, b, tolerance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float blend)
        {
            return MathHelper.Lerp(a, b, blend);
        }
    }
}
