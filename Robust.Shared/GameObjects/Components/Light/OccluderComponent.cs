using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;
using System.Numerics;

namespace Robust.Shared.GameObjects;

[RegisterComponent]
[NetworkedComponent()]
[AutoGenerateComponentState(true)]
[Access(typeof(OccluderSystem))]
public sealed partial class OccluderComponent : Component, IComponentTreeEntry<OccluderComponent>
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Local-space convex polygon vertices. Vertices may be specified in any order; serialization normalizes them
    /// through the physics hull implementation.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PhysicsHullSerializer)), AutoNetworkedField]
    private Vector2[] _polygon =
    [
        new(-0.5f, 0.5f),
        new(0.5f, 0.5f),
        new(0.5f, -0.5f),
        new(-0.5f, -0.5f),
    ];

    public ReadOnlySpan<Vector2> Polygon => _polygon;

    internal Vector2[] PolygonArray
    {
        get => _polygon;
        set => _polygon = value;
    }

    /// <summary>
    /// Cached local-space bounds for <see cref="Polygon"/>.
    /// </summary>
    [ViewVariables]
    public Box2 LocalBounds { get; internal set; } = Box2.Empty; // Leave as empty so we remember to always update the cache on init.

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<OccluderComponent>>? Tree { get; set; }

    public bool AddToTree => Enabled;
    public bool TreeUpdateQueued { get; set; } = false;
}
