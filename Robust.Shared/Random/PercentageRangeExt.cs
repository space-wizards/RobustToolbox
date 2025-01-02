using Robust.Shared.Maths;

namespace Robust.Shared.Random;

public static class PercentageRangeExt
{
    public static float NextFloat(this PercentageRange range, IRobustRandom random)
    {
        return random.NextFloat(range.Min, range.Max);
    }
}
