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
        private int radius;
        private Vector2 offset;

        public int Radius { get => radius; set => radius = value; }
        public Vector2 Offset { get => offset; set => offset = value; }

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
