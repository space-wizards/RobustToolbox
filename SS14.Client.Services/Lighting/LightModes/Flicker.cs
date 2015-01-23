using GorgonLibrary;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Utility;
using SS14.Shared;
using SS14.Shared.IoC;
using System.Drawing;

namespace SS14.Client.GameObjects.Component.Light.LightModes
{
    public class LightFlicker : LightMode
    {
        private readonly PreciseTimer timer = new PreciseTimer();
        private Color _lightColorOriginal;
        private int flickerCount;
        private bool flickering;
        private LightModeClass lightModeClass = LightModeClass.Flicker;
        private bool lightOn = true;

        #region LightMode Members

        public LightModeClass LightModeClass
        {
            get { return lightModeClass; }
            set { lightModeClass = value; }
        }

        //Since lightcolor is only saved when added, changes made during the effects of this mode will reset. FIX THIS.

        public void OnAdd(ILight owner)
        {
            _lightColorOriginal = owner.Color;
            timer.Reset();
        }

        public void OnRemove(ILight owner)
        {
            owner.SetColor(_lightColorOriginal);
            owner.LightArea.Calculated = false;
        }

        public void Update(ILight owner, float frametime)
        {
            if (flickering)
            {
                if (lightOn == false)
                {
                    if (timer.Milliseconds >= 50 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                    {
                        flickerCount++;
                        lightOn = true;
                        owner.SetColor(_lightColorOriginal);
                        owner.LightArea.Calculated = false;
                        timer.Reset();
                        if (flickerCount >= 2 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                            flickering = false;
                    }
                }
                else if (timer.Milliseconds >= 50 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                {
                    owner.SetColor((_lightColorOriginal.A/2), (_lightColorOriginal.R/2), (_lightColorOriginal.G/2),
                                   (_lightColorOriginal.B/2));
                    owner.LightArea.Calculated = false;
                    lightOn = false;
                    timer.Reset();
                }
            }
            else
            {
                if (timer.Seconds >= 4 && IoCManager.Resolve<IRand>().Next(1, 5) == 2)
                {
                    flickering = true;
                    flickerCount = 0;
                    lightOn = false;
                    owner.SetColor((_lightColorOriginal.A/2), (_lightColorOriginal.R/2), (_lightColorOriginal.G/2),
                                   (_lightColorOriginal.B/2));
                    owner.LightArea.Calculated = false;
                    timer.Reset();
                }
            }
        }

        #endregion
    }
}