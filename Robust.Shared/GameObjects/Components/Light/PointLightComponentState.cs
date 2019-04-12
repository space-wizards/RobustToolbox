using System;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class PointLightComponentState : ComponentState
    {
        public readonly Color Color;
        public readonly LightModeClass Mode;
        public readonly LightState State;

        public readonly float Radius;
        public readonly Vector2 Offset;

        public PointLightComponentState(LightState state, Color color, LightModeClass mode, float radius, Vector2 offset)
            : base(NetIDs.POINT_LIGHT)
        {
            State = state;
            Color = color;
            Mode = mode;
            Radius = radius;
            Offset = offset;
        }
    }
}
