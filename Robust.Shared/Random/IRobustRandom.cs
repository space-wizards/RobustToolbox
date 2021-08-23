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
        void NextBytes(byte[] buffer);

        public Angle NextAngle() => NextFloat() * MathHelper.Pi * 2;
        public Angle NextAngle(Angle minValue, Angle maxValue) => NextFloat() * (maxValue - minValue) + minValue;
        public Angle NextAngle(Angle maxValue) => NextFloat() * maxValue;

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
