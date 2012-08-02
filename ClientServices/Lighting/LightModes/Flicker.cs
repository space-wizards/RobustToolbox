using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces.GOC;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ClientInterfaces;
using ClientInterfaces.Utility;

namespace CGO.Component.Light.LightModes
{
    public class LightFlicker : LightMode
    {
        public LightModeClass LightModeClass
        {
            get
            {
                return lightModeClass;
            }
            set
            {
                lightModeClass = value;
            }
        }
        private LightModeClass lightModeClass = LightModeClass.Flicker;

        private Color _lightColorOriginal;
        private PreciseTimer timer = new PreciseTimer();
        private bool flickering = false;
        private bool lightOn = true;
        private int flickerCount = 0;

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
                    owner.SetColor((int)(_lightColorOriginal.A / 2), (int)(_lightColorOriginal.R / 2), (int)(_lightColorOriginal.G / 2), (int)(_lightColorOriginal.B / 2));
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
                    owner.SetColor((int)(_lightColorOriginal.A / 2), (int)(_lightColorOriginal.R / 2), (int)(_lightColorOriginal.G / 2), (int)(_lightColorOriginal.B / 2));
                    owner.LightArea.Calculated = false;
                    timer.Reset();
                }
            }
        }
    }
}
