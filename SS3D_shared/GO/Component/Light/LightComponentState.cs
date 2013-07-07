using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Component.Light
{
    [Serializable]
    public class LightComponentState: ComponentState
    {
        public LightState State;
        public int ColorR;
        public int ColorG;
        public int ColorB;
        public LightModeClass Mode;

        public LightComponentState(LightState state, int colorR, int colorG, int colorB, LightModeClass mode)
            :base(ComponentFamily.Light)
        {
            State = state;
            ColorR = colorR;
            ColorG = colorG;
            ColorB = colorB;
            Mode = mode;
        }
    }
}
