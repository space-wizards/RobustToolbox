using System;

namespace SS14.Shared.GameObjects.Components.Light
{
    [Serializable]
    public class LightComponentState : ComponentState
    {
        public int ColorB;
        public int ColorG;
        public int ColorR;
        public LightModeClass Mode;
        public LightState State;

        public LightComponentState(LightState state, int colorR, int colorG, int colorB, LightModeClass mode)
            : base(ComponentFamily.Light)
        {
            State = state;
            ColorR = colorR;
            ColorG = colorG;
            ColorB = colorB;
            Mode = mode;
        }
    }
}
