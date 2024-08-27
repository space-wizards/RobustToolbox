using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics;

internal sealed class PhysicsWorld
{
    public bool AutoClearForces;

    /// <summary>
    /// When substepping the client needs to know about the first position to use for lerping.
    /// </summary>
    public readonly Dictionary<EntityUid, (EntityUid ParentUid, Vector2 LocalPosition, Angle LocalRotation)>
        LerpData = new();

    /// <summary>
    /// Keep a buffer of everything that moved in a tick. This will be used to check for physics contacts.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<FixtureProxy, Box2> MoveBuffer = new();

    /// <summary>
    ///     All awake bodies on this map.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<PhysicsComponent> AwakeBodies = new();

    /// <summary>
    ///     Store last tick's invDT
    /// </summary>
    internal float _invDt0;
}
