using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class PointLightComponentState : ComponentState
    {
        public readonly int ColorB;
        public readonly int ColorG;
        public readonly int ColorR;
        public readonly LightModeClass Mode;
        public readonly LightState State;

        public PointLightComponentState(LightState state, int colorR, int colorG, int colorB, LightModeClass mode)
            : base(NetIDs.POINT_LIGHT)
        {
            State = state;
            ColorR = colorR;
            ColorG = colorG;
            ColorB = colorB;
            Mode = mode;
        }
    }
}
