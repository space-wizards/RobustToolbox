using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedEyeSystem)), AutoGenerateComponentState(true, fieldDeltas: true)]
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
        [DataField, AutoNetworkedField]
        public EntityUid? Target;

        [DataField, AutoNetworkedField]
        public bool DrawFov = true;

        [AutoNetworkedField]
        public bool DrawLight = true;

        // yes it's not networked, don't ask.
        [DataField]
        public Angle Rotation;

        [DataField]
        public Vector2 Zoom = Vector2.One;

        /// <summary>
        /// Eye offset, relative to the map, and not affected by <see cref="Rotation"/>
        /// </summary>
        [DataField, AutoNetworkedField]
        public Vector2 Offset;

        /// <summary>
        ///     The visibility mask for this eye.
        ///     The player will be able to get updates for entities whose layers match the mask.
        /// </summary>
        [DataField("visMask", customTypeSerializer:typeof(FlagSerializer<VisibilityMaskLayer>)), AutoNetworkedField]
        public int VisibilityMask = DefaultVisibilityMask;

        /// <summary>
        /// Scaling factor for the PVS view range of this eye. This effectively allows the
        /// <see cref="CVars.NetMaxUpdateRange"/> and <see cref="CVars.NetPvsPriorityRange"/> cvars to be configured per
        /// eye.
        /// </summary>
        [Access(typeof(SharedEyeSystem))]
        [DataField]
        public float PvsScale = 1;
    }

    /// <summary>
    /// Single layer used for Eye visibility. Controls what entities they are allowed to see.
    /// </summary>
    public sealed class VisibilityMaskLayer {}
}
