using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    public abstract class EyeComponent : Component
    {
        public override string Name => "Eye";
        public override uint? NetID => NetIDs.EYE;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool DrawFov { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Vector2 Zoom { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Vector2 Offset { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Angle Rotation { get; set; }

        // Basically I needed a way to chain this effect for the attack lunge animation.
        // Sorry!
        public Vector2 BaseOffset { get; set; }

        public abstract void Kick(Vector2 recoil);

        [Serializable, NetSerializable]
        protected class RecoilKickMessage : ComponentMessage
        {
            public readonly Vector2 Recoil;

            public RecoilKickMessage(Vector2 recoil)
            {
                Directed = true;
                Recoil = recoil;
            }
        }
    }

    [NetSerializable, Serializable]
    public class EyeComponentState : ComponentState
    {
        public bool DrawFov { get; }
        public Vector2 Zoom { get; }
        public Vector2 Offset { get; }
        public Angle Rotation { get; }

        public EyeComponentState(bool drawFov, Vector2 zoom, Vector2 offset, Angle rotation) : base(NetIDs.EYE)
        {
            DrawFov = drawFov;
            Zoom = zoom;
            Offset = offset;
            Rotation = rotation;
        }
    }
}
