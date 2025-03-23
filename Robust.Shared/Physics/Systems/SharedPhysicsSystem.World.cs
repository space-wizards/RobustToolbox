using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    // Box2D has a bunch of methods that work on worlds but in our case separate EntityManager instances are
    // separate worlds so we can just treat the physics system as the world.

    public bool AutoClearForces;

    /// <summary>
    /// When substepping the client needs to know about the first position to use for lerping.
    /// </summary>
    public readonly Dictionary<EntityUid, (EntityUid ParentUid, Vector2 LocalPosition, Angle LocalRotation)>
        LerpData = new();

    // TODO: MoveBuffer should be broadphase local
    // Remove recursive grid from entitylookupsystem
    // Need to triplecheclk that one test

    // Previously we stored the WorldAABB of the proxy being moved and tracked state.
    // The issue is that if something moves multiple times in a tick it can add up, plus it's also done on hotpaths such as physics.
    // As such we defer it until we actually try and get contacts, then we can run them in parallel.
    /// <summary>
    /// Keep a buffer of everything that moved in a tick. This will be used to check for physics contacts.
    /// </summary>
    [ViewVariables]
    internal readonly HashSet<FixtureProxy> MoveBuffer = new();

    /// <summary>
    /// Track moved grids to know if we need to run checks for them driving over entities.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> MovedGrids = new();

    /// <summary>
    ///     All awake bodies on this map.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<Entity<PhysicsComponent, TransformComponent>> AwakeBodies = new();

    /// <summary>
    ///     Store last tick's invDT
    /// </summary>
    internal float _invDt0;
}
