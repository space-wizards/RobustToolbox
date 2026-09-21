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
    /// Whether vision raycasts (<see cref="OccluderSystem.InRangeUnoccluded"/>) hit this occluder.
    /// Independent of FOV / light mesh contribution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockVision = true;

    /// <summary>
    /// Whether this occluder is drawn into the hard FOV mesh.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DrawFov = true;

    /// <summary>
    /// Whether this occluder contributes to light-shadow / wall-bleed occlusion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockLight = true;

    /// <summary>
    /// Optional tall lighting size in local tiles (width, height). Hard FOV and vision always use
    /// <see cref="Polygon"/>. When set, lighting draws the gameplay footprint plus an extra-height strip.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 VisualSize;

    /// <summary>
    /// Center offset applied to <see cref="VisualSize"/> when caching <see cref="VisualLocalBounds"/>
    /// (tree queries). Strip lighting uses <see cref="VisualSize"/>.Y against the footprint height.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 VisualOffset;

    /// <summary>
    /// When true with <see cref="VisualSize"/>, the lighting strip follows this client's snapCardinals
    /// screen-up. Hard FOV stays on <see cref="Polygon"/>. Pair with sprite <c>snapCardinals: true</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlignVisualToEye;

    /// <summary>
    /// Local-space convex polygon vertices used for vision / FOV / shared-edge topology.
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
    public Box2 LocalBounds { get; internal set; } = Box2.Empty;

    /// <summary>
    /// Cached local-space bounds for <see cref="VisualSize"/> / <see cref="VisualOffset"/>, or empty.
    /// </summary>
    [ViewVariables]
    public Box2 VisualLocalBounds { get; internal set; } = Box2.Empty;

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
