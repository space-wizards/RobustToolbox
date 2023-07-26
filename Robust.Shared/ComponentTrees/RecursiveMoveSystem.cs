using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.ComponentTrees;

/// <summary>
///     This system will recursively raise events to update component tree positions any time any entity moves.
/// </summary>
/// <remarks>
///     This is used by some client-side systems (e.g., sprites, lights, etc). However this can be quite expensive and if possible should not be used by the server.
/// </remarks>
internal sealed class RecursiveMoveSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    bool Subscribed = false;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    internal void AddSubscription()
    {
        if (Subscribed)
            return;

        Subscribed = true;
        SubscribeLocalEvent<MoveEvent>(AnythingMoved);
    }

    private void AnythingMoved(ref MoveEvent args)
    {
        if (args.Component.MapUid == args.Sender || args.Component.GridUid == args.Sender)
            return;

        DebugTools.Assert(!_mapManager.IsMap(args.Sender));
        DebugTools.Assert(!_mapManager.IsGrid(args.Sender));

        AnythingMovedSubHandler(args.Sender, args.Component);
    }

    private void AnythingMovedSubHandler(
        EntityUid uid,
        TransformComponent xform)
    {
        // TODO maybe use a c# event? This event gets raised a lot.
        // Would probably help with server performance and is also the main bottleneck for replay scrubbing.
        var ev = new TreeRecursiveMoveEvent(xform);
        RaiseLocalEvent(uid, ref ev);

        // TODO only enumerate over entities in containers if necessary?
        // annoyingly, containers aren't guaranteed to occlude sprites & lights
        // but AFAIK thats currently unused???

        var childEnumerator = xform.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            if (_xformQuery.TryGetComponent(child.Value, out var childXform))
                AnythingMovedSubHandler(child.Value, childXform);
        }
    }
}
