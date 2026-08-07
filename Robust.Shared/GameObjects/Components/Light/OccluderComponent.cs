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
[Access(typeof(OccluderSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class OccluderComponent : Component, IComponentTreeEntry<OccluderComponent>
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Local-space convex polygon vertices.
    /// </summary>
    [DataField("polygon", customTypeSerializer: typeof(PhysicsHullSerializer)), AutoNetworkedField]
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

    /// <summary>
    /// Cached client-side shared-edge mask. Bit <c>i</c> is set when polygon render edge <c>i</c> is exactly shared
    /// with another enabled occluder edge.
    /// </summary>
    [ViewVariables]
    public byte OccludingEdges;

    /// <summary>
    /// Last tree-local bounds used to dirty neighbours when this occluder moves, changes polygon, or is removed.
    /// </summary>
    [ViewVariables]
    public (EntityUid TreeUid, Box2 Bounds)? LastTreeBounds;
}
