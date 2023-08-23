using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Robust.Shared.GameObjects;

[RegisterComponent]
[NetworkedComponent()]
[Access(typeof(OccluderSystem))]
public sealed partial class OccluderComponent : Component, IComponentTreeEntry<OccluderComponent>
{
    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("boundingBox")]
    public Box2 BoundingBox = new(-0.5f, -0.5f, 0.5f, 0.5f);

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<OccluderComponent>>? Tree { get; set; }

    public bool AddToTree => Enabled;
    public bool TreeUpdateQueued { get; set; } = false;

    [ViewVariables] public (EntityUid Grid, Vector2i Tile)? LastPosition;
    [ViewVariables] public OccluderDir Occluding;

    [Flags]
    public enum OccluderDir : byte
    {
        None = 0,
        North = 1,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
    }

    [NetSerializable, Serializable]
    public sealed class OccluderComponentState : ComponentState
    {
        public bool Enabled { get; }
        public Box2 BoundingBox { get; }

        public OccluderComponentState(bool enabled, Box2 boundingBox)
        {
            Enabled = enabled;
            BoundingBox = boundingBox;
        }
    }
}
