using OpenTK;
using OpenTK.Graphics;
using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class PointLightComponentState : ComponentState
    {
        public readonly Color4 Color;
        public readonly LightModeClass Mode;
        public readonly LightState State;

        public int Radius { get; set; }
        public Vector2 Offset { get; set; }

        public PointLightComponentState(LightState state, Color4 color, LightModeClass mode, int radius, Vector2 offset)
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
