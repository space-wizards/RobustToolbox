using Robust.Shared.GameObjects;

namespace Robust.Shared.ComponentTrees;

[ByRefEvent]
internal readonly struct TreeRecursiveMoveEvent
{
    public readonly TransformComponent Xform;
    public TreeRecursiveMoveEvent(TransformComponent xform)
    {
        Xform = xform;
    }
}
