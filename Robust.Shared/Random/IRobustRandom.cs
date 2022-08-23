using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Random
{
    public interface IRobustRandom
    {
        float NextFloat();
        public float NextFloat(float minValue, float maxValue)
            => NextFloat() * (maxValue - minValue) + minValue;
        public float NextFloat(float maxValue) => NextFloat() * maxValue;
        int Next();
        int Next(int minValue, int maxValue);
        int Next(int maxValue);
        double NextDouble();
        double NextDouble(double minValue, double maxValue) => NextDouble() * (maxValue - minValue) + minValue;
        void NextBytes(byte[] buffer);

        public Angle NextAngle() => NextFloat() * MathHelper.Pi * 2;
        public Angle NextAngle(Angle minValue, Angle maxValue) => NextFloat() * (maxValue - minValue) + minValue;
        public Angle NextAngle(Angle maxValue) => NextFloat() * maxValue;

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <remarks>
        ///     In general, NextVector2(1) will tend to result in vectors with smaller magnitudes than
        ///     NextVector2Box(1,1), even if you ignored any vectors with a magnitude larger than one.
        /// </remarks>
        public Vector2 NextVector2(float minMagnitude, float maxMagnitude) => NextAngle().RotateVec((NextFloat(minMagnitude, maxMagnitude), 0));
        public Vector2 NextVector2(float maxMagnitude = 1) => NextVector2(0, maxMagnitude);

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        /// </summary>
        public Vector2 NextVector2Box(float minX, float minY, float maxX, float maxY) => new Vector2(NextFloat(minX, maxX), NextFloat(minY, maxY));
        public Vector2 NextVector2Box(float maxAbsX = 1, float maxAbsY = 1) => NextVector2Box(-maxAbsX, -maxAbsY, maxAbsX, maxAbsY);

        void Shuffle<T>(IList<T> list)
        {
            var n = list.Count;
            while (n > 1) {
                n -= 1;
                var k = Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
