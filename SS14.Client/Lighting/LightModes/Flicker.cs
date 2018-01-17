using System.Diagnostics;
using OpenTK.Graphics;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Interfaces.Utility;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.Light.LightModes
{
    public class LightFlicker : LightMode
    {
        private readonly Stopwatch timer = new Stopwatch();
        private Color4 _lightColorOriginal;
        private int flickerCount;
        private bool flickering;
        private bool lightOn = true;

        public LightModeClass LightModeClass { get; set; } = LightModeClass.Flicker;

        //Since lightcolor is only saved when added, changes made during the effects of this mode will reset. FIX THIS.

        public void OnAdd(ILight owner)
        {
            _lightColorOriginal = owner.Color;
            timer.Reset();
        }

        public void OnRemove(ILight owner)
        {
            owner.Color = _lightColorOriginal;
            owner.Calculated = false;
        }

        public void Update(ILight owner, float deltaTime)
        {
            if (flickering)
            {
                if (lightOn == false)
                {
                    if (timer.ElapsedMilliseconds >= 50 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                    {
                        flickerCount++;
                        lightOn = true;
                        owner.Color = _lightColorOriginal;
                        owner.Calculated = false;
                        timer.Reset();
                        if (flickerCount >= 2 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                            flickering = false;
                    }
                }
                else if (timer.ElapsedMilliseconds >= 50 && IoCManager.Resolve<IRand>().Next(1, 6) == 2)
                {
                    owner.Color = new Color4(_lightColorOriginal.R / 2, _lightColorOriginal.G / 2,
                        _lightColorOriginal.B / 2, _lightColorOriginal.A / 2);
                    owner.Calculated = false;
                    lightOn = false;
                    timer.Reset();
                }
            }
            else
            {
                if (timer.Elapsed.Seconds >= 4 && IoCManager.Resolve<IRand>().Next(1, 5) == 2)
                {
                    flickering = true;
                    flickerCount = 0;
                    lightOn = false;
                    owner.Color = new Color4(_lightColorOriginal.R / 2, _lightColorOriginal.G / 2,
                        _lightColorOriginal.B / 2, _lightColorOriginal.A / 2);
                    owner.Calculated = false;
                    timer.Reset();
                }
            }
        }
    }
}
