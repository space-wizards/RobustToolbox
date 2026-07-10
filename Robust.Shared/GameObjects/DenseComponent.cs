using System.Threading;

namespace Robust.Shared.GameObjects;

internal static class DenseComponent
{
    public static int Index = -1;
}

internal static class DenseComponent<T>
{
    // ReSharper disable once StaticMemberInGenericType
    internal static readonly int Index = Interlocked.Increment(ref DenseComponent.Index);

    static DenseComponent()
    {
        CompIdx.SetDense<T>(true, Index);
    }
}
