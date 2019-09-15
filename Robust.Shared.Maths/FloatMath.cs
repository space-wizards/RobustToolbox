using System;

namespace Robust.Shared.Maths
{
    public static class FloatMath
    {
        public const float RadToDeg = (float) (180.0 / Math.PI);
        public const float DegToRad = (float) (Math.PI / 180.0);

        /// <summary>
        /// Returns the largest integer smaller to or equal to f.
        /// </summary>
#if NETCOREAPP
        public static float Floor(float f) => MathF.Floor(f);
#else
        public static float Floor(float f) => (float)Math.Floor(f);
#endif

        public static T Clamp<T>(this T val, T min, T max)
            where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        public static bool CloseTo(float a, float b, double tolerance = .00001)
        {
            var epsilon =
                Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * tolerance,
                    tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(float a, double b, double tolerance = .00001)
        {
            var epsilon =
                Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * tolerance,
                    tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(double a, float b, double tolerance = .00001)
        {
            var epsilon =
                Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * tolerance,
                    tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(double a, double b, double tolerance = .00001)
        {
            var epsilon =
                Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * tolerance,
                    tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(a - b) <= epsilon;
        }

        /// <summary>
        ///     <c>blend</c> 0 means <c>a</c>
        /// </summary>
        public static double Lerp(double a, double b, double blend)
        {
            return a + (b - a) * blend;
        }

        /// <summary>
        ///     <c>blend</c> 0 means <c>a</c>
        /// </summary>
        public static float Lerp(float a, float b, float blend)
        {
            return a + (b - a) * blend;
        }

        public static float InterpolateCubic(float preA, float a, float b, float postB, float t)
        {
            return a + 0.5f * t *
                   (b - preA + t * (2.0f * preA - 5.0f * a + 4.0f * b - postB + t * (3.0f * (a - b) + postB - preA)));
        }

        public static double InterpolateCubic(double preA, double a, double b, double postB, double t)
        {
            return a + 0.5 * t *
                   (b - preA + t * (2.0 * preA - 5.0 * a + 4.0 * b - postB + t * (3.0 * (a - b) + postB - preA)));
        }

        // Clamps value between 0 and 1 and returns value
        public static float Clamp01(float value)
        {
            if (value < 0F)
                return 0F;

            if (value > 1F)
                return 1F;

            return value;
        }

        // Loops the value t, so that it is never larger than length and never smaller than 0.
        public static float Repeat(float t, float length)
        {
            return Clamp(t - Floor(t / length) * length, 0.0f, length);
        }
    }
}
