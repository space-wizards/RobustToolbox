using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedEyeSystem)), AutoGenerateComponentState(true)]
    public sealed partial class EyeComponent : Component
    {
        public const int DefaultVisibilityMask = 1;

        [ViewVariables]
        public readonly Eye Eye = new();

        /// <summary>
        ///     If not null, this entity is used to update the eye's position instead of just using the component's owner.
        /// </summary>
        /// <remarks>
        ///     This is useful for things like vehicles that effectively need to hijack the eye. This allows them to do
        ///     that without messing with the main viewport's eye. This is important as there are some overlays that are
        ///     only be drawn if that viewport's eye belongs to the currently controlled entity.
        /// </remarks>
        [ViewVariables, DataField("target"), AutoNetworkedField]
        public EntityUid? Target;

        [ViewVariables(VVAccess.ReadWrite), DataField("drawFov"), AutoNetworkedField]
        public bool DrawFov = true;

        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public bool DrawLight = true;

        // yes it's not networked, don't ask.
        [ViewVariables(VVAccess.ReadWrite), DataField("rotation")]
        public Angle Rotation;

        [ViewVariables(VVAccess.ReadWrite), DataField("zoom")]
        public Vector2 Zoom = Vector2.One;

        [ViewVariables(VVAccess.ReadWrite), DataField("offset"), AutoNetworkedField]
        public Vector2 Offset;

        /// <summary>
        ///     The visibility mask for this eye.
        ///     The player will be able to get updates for entities whose layers match the mask.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("visMask", customTypeSerializer:typeof(FlagSerializer<VisibilityMaskLayer>)), AutoNetworkedField]
        public int VisibilityMask = DefaultVisibilityMask;

        /// <summary>
        /// Overrides the PVS view range of this eye, Effectively a per-eye <see cref="CVars.NetMaxUpdateRange"/> cvar.
        /// </summary>
        [DataField] public float? PvsSize;
    }

    /// <summary>
    /// Single layer used for Eye visibility. Controls what entities they are allowed to see.
    /// </summary>
    public sealed class VisibilityMaskLayer {}
}
