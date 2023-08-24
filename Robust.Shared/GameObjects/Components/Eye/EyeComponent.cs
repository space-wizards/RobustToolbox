using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedEyeSystem)), AutoGenerateComponentState]
    public sealed partial class EyeComponent : Component
    {
        #region Client

        [ViewVariables] internal Eye? _eye = default!;

        public IEye? Eye => _eye;

        [ViewVariables]
        public MapCoordinates? Position => _eye?.Position;

        #endregion

        public const int DefaultVisibilityMask = 1;

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
        public bool DrawFov;

        [ViewVariables(VVAccess.ReadWrite), DataField("zoom"), AutoNetworkedField]
        public Vector2 Zoom;

        [ViewVariables(VVAccess.ReadWrite), DataField("offset"), AutoNetworkedField]
        public Vector2 Offset;

        [ViewVariables(VVAccess.ReadWrite), DataField("rotation"), AutoNetworkedField]
        public Angle Rotation;

        /// <summary>
        ///     The visibility mask for this eye.
        ///     The player will be able to get updates for entities whose layers match the mask.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("visMask"), AutoNetworkedField]
        public uint VisibilityMask = DefaultVisibilityMask;
    }
}
