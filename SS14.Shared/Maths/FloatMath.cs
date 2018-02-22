using System;


namespace SS14.Shared.Maths
{
    public static class FloatMath
    {

        private const int LookupSize = 1024 * 64; //has to be power of 2
        private static readonly float[] getSin, getCos;

        static FloatMath()
        {
            getSin = new float[LookupSize];
            getCos = new float[LookupSize];

            for (var i = 0; i < LookupSize; i++)
            {
                getSin[i] = (float)Math.Sin(i * Math.PI / LookupSize * 2);
                getCos[i] = (float)Math.Cos(i * Math.PI / LookupSize * 2);
            }
        }

        /// <summary>
        /// Fast innacurate sinus
        /// </summary>
        public static float Sin(float degrees)
        {
            var rot = GetIndex(degrees);
            return getSin[rot];
        }

        /// <summary>
        /// Fast innacurate cosinus
        /// </summary>
        public static float Cos(float degrees)
        {
            var rot = GetIndex(degrees);
            return getCos[rot];
        }

        static int GetIndex(float degrees)
        {
            return (int)(degrees * (LookupSize / 360f) + 0.5f) & (LookupSize - 1);
        }
        public static void SinCos(float degrees, out float sin, out float cos)
        {
            var rot = GetIndex(degrees);

            sin = getSin[rot];
            cos = getCos[rot];
        }

        public const float Pi = (float)Math.PI;

        public static float ToDegrees(float radians)
        {
            return radians / Pi * 180;
        }
        public static float ToRadians(float degrees)
        {
            return degrees / 180 * Pi;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static bool CloseTo(float A, float B)
        {
            var epsilon = Math.Max(Math.Max(Math.Abs(A), Math.Abs(B)) * 0.00001, .00001); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(A - B) <= epsilon;
        }

        public static bool CloseTo(float A, double B)
        {
            var epsilon = Math.Max(Math.Max(Math.Abs(A), Math.Abs(B)) * 0.00001, .00001); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(A - B) <= epsilon;
        }

        public static bool CloseTo(double A, float B)
        {
            var epsilon = Math.Max(Math.Max(Math.Abs(A), Math.Abs(B)) * 0.00001, .00001); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(A - B) <= epsilon;
        }

        public static bool CloseTo(double A, double B)
        {
            var epsilon = Math.Max(Math.Max(Math.Abs(A), Math.Abs(B)) * 0.00001, .00001); // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            return Math.Abs(A - B) <= epsilon;
        }
    }
}

