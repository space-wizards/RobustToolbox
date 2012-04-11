using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using SS13_Shared;

namespace ClientServices.Lighting
{
    public class LightManager : ILightManager
    {
        private List<ILight> _lights = new List<ILight>();

        public void AddLight(ILight light)
        {
            if(!_lights.Contains(light))
                _lights.Add(light);
        }

        public void RemoveLight(ILight light)
        {
            if(_lights.Contains(light))
                _lights.Remove(light);
        }

        public ILight[] lightsInRadius(Vector2D point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Position - point).Length) <= radius).ToArray();
        }

        public ILight CreateLight()
        {
            return new Light();
        }

        public void RecalculateLights()
        {
            foreach(var l in _lights)
            {
                l.LightArea.Calculated = false;
            }
        }
    }

}