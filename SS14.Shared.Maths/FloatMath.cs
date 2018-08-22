using System;

namespace SS14.Shared.Maths
{
    public static class FloatMath
    {
        private const int LookupSize = 1024 * 64; //has to be power of 2
        private static readonly float[] getSin, getCos;
        public const float RadToDeg = (float)(180.0 / System.Math.PI);
        public const float DegToRad = (float)(System.Math.PI / 180.0);

        static FloatMath()
        {
            getSin = new float[LookupSize];
            getCos = new float[LookupSize];

            for (var i = 0; i < LookupSize; i++)
            {
                getSin[i] = (float)System.Math.Sin(i * System.Math.PI / LookupSize * 2);
                getCos[i] = (float)System.Math.Cos(i * System.Math.PI / LookupSize * 2);
            }
        }

        /// <summary>
        /// Fast inaccurate sinus
        /// </summary>
        public static float Sin(float degrees)
        {
            var rot = GetIndex(degrees);
            return getSin[rot];
        }

        /// <summary>
        /// Fast inaccurate cosinus
        /// </summary>
        public static float Cos(float degrees)
        {
            var rot = GetIndex(degrees);
            return getCos[rot];
        }

        public static int GetIndex(float degrees)
        {
            return (int)(degrees * (LookupSize / 360f) + 0.5f) & (LookupSize - 1);
        }

        public static void SinCos(float degrees, out float sin, out float cos)
        {
            var rot = GetIndex(degrees);

            sin = getSin[rot];
            cos = getCos[rot];
        }

        public static float Min(float a, float b)
        {
            return System.Math.Min(a, b);
        }

        public static float Max(float a, float b)
        {
            return System.Math.Max(a, b);
        }

        public const float Pi = (float)System.Math.PI;

        public static float ToDegrees(float radians)
        {
            return radians / Pi * 180;
        }
        public static float ToRadians(float degrees)
        {
            return degrees / 180 * Pi;
        }

        public static T Clamp<T>(this T val, T min, T max)
            where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        public static bool CloseTo(float a, float b, double tolerance = .00001)
        {
            var epsilon = System.Math.Max(System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)) * tolerance, tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return System.Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(float a, double b, double tolerance = .00001)
        {
            var epsilon = System.Math.Max(System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)) * tolerance, tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return System.Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(double a, float b, double tolerance = .00001)
        {
            var epsilon = System.Math.Max(System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)) * tolerance, tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return System.Math.Abs(a - b) <= epsilon;
        }

        public static bool CloseTo(double a, double b, double tolerance = .00001)
        {
            var epsilon = System.Math.Max(System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)) * tolerance, tolerance); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return System.Math.Abs(a - b) <= epsilon;
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
    }
}
