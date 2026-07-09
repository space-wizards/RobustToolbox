using System.Threading;

namespace Robust.Shared.GameObjects;

internal static class ArchetypeComponent
{
    public static int Index = -1;
}

internal static class ArchetypeComponent<T>
{
    // ReSharper disable once StaticMemberInGenericType
    internal static readonly int Index = Interlocked.Increment(ref ArchetypeComponent.Index);

    static ArchetypeComponent()
    {
        CompIdx.SetArchetype<T>(true, Index);
    }
}
