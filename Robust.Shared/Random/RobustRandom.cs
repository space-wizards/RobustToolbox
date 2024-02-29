using System;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Random
{
    public sealed class RobustRandom : IRobustRandom
    {
        private System.Random _random = new();

        public System.Random GetRandom() => _random;

        public void SetSeed(int seed)
        {
            _random = new(seed);
        }

        public float NextFloat()
        {
            return _random.NextFloat();
        }

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public TimeSpan Next(TimeSpan minTime, TimeSpan maxTime)
        {
            DebugTools.Assert(minTime < maxTime);
            return minTime + (maxTime - minTime) * _random.NextDouble();
        }

        public TimeSpan Next(TimeSpan maxTime)
        {
            return Next(TimeSpan.Zero, maxTime);
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

        public void NextBytes(byte[] buffer)
        {
            _random.NextBytes(buffer);
        }

        public T? SelectRandomOrDefault<T>(List<T> list) where T : new()
        {
            return list.Count == 0 ? default : list[Next(list.Count)];
        }

        public T SelectRandom<T>(List<T> list) where T : new()
        {
            if (list.Count == 0)
            {
                throw new ArgumentException("Specified List does not have any elements");
            }
            return list[Next(list.Count)];
        }
    }
}
