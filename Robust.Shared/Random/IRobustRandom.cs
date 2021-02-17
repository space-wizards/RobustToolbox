using System.Collections.Generic;

namespace Robust.Shared.Random
{
    public interface IRobustRandom
    {
        int Next();
        int Next(int minValue, int maxValue);
        int Next(int maxValue);
        double NextDouble();
        void NextBytes(byte[] buffer);

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
