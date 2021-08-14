using System;
using Robust.Shared.Serialization;

namespace Robust.Benchmarks.Serialization.Definitions
{
    public class BenchmarkFlags
    {
        public const int Zero = 1 << 0;
        public const int ThirtyOne = 1 << 31;
    }

    [Flags]
    [FlagsFor(typeof(BenchmarkFlags))]
    public enum BenchmarkFlagsEnum
    {
        Zero = BenchmarkFlags.Zero,
        ThirtyOne = BenchmarkFlags.ThirtyOne
    }
}
