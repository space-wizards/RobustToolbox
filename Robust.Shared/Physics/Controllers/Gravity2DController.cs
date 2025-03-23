using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Controllers;

public sealed class Gravity2DController : VirtualController
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private ISawmill _sawmill = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("physics");

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public Vector2 GetGravity(EntityUid uid, Gravity2DComponent? component = null)
    {
        return PhysicsSystem.Gravity;

        if (!Resolve(uid, ref component, false))
            return Vector2.Zero;

        return component.Gravity;
    }

    public void SetGravity(EntityUid uid, Vector2 value)
    {
        PhysicsSystem.Gravity = value;

        if (!HasComp<MapComponent>(uid))
        {
            _sawmill.Error($"Tried to set 2D gravity for an entity that isn't a map?");
            DebugTools.Assert(false);
            return;
        }

        var gravity = EnsureComp<Gravity2DComponent>(uid);

        if (gravity.Gravity.Equals(value))
            return;

        gravity.Gravity = value;
        WakeBodiesRecursive(uid);
        Dirty(uid, gravity);
    }

    public void SetGravity(MapId mapId, Vector2 value)
    {
        PhysicsSystem.Gravity = value;
        var mapUid = _mapSystem.GetMap(mapId);
        var gravity = EnsureComp<Gravity2DComponent>(mapUid);

        if (gravity.Gravity.Equals(value))
            return;

        gravity.Gravity = value;
        WakeBodiesRecursive(mapUid);
        Dirty(mapUid, gravity);
    }

    private void WakeBodiesRecursive(EntityUid uid)
    {
        if (_physicsQuery.TryGetComponent(uid, out var body) &&
            body.BodyType == BodyType.Dynamic)
        {
            _physics.WakeBody(uid, body: body);
        }

        var xform = EntityManager.TransformQuery.GetComponent(uid);
        foreach (var child in xform._children)
        {
            WakeBodiesRecursive(child);
        }
    }
}
