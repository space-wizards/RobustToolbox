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
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;
using CGO.Component.Light.LightModes;

namespace ClientServices.Lighting
{
    public class LightManager : ILightManager
    {
        private List<ILight> _lights = new List<ILight>();
        List<Type> LightModes = new List<Type>();

        public LightManager()
        {
            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LightModes = assemblies.SelectMany(t => t.GetTypes()).Where(p => typeof(LightMode).IsAssignableFrom(p) && !p.IsInterface).ToList();
        }

        public void SetLightMode(LightModeClass? mode, ILight light)
        {
            if (!mode.HasValue)
            {
                if (light.LightMode != null)
                {
                    light.LightMode.OnRemove(light);
                    light.LightMode = null;
                }
                return;
            }

            List<LightMode> modes = new List<LightMode>();
            foreach (Type t in LightModes)
            {
                LightMode temp = (LightMode)Activator.CreateInstance(t);
                if (temp.LightModeClass == mode.Value)
                {
                    if (light.LightMode != null)
                    {
                        light.LightMode.OnRemove(light);
                        light.LightMode = null;
                    }
                    light.LightMode = temp;
                    temp.OnAdd(light);
                    return;
                }
            }
        }

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

        public ILight[] GetLights()
        {
            return _lights.ToArray();
        }

        public ILight[] lightsInRadius(Vector2D point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Position - point).Length) <= radius).ToArray();
        }

        public ILight[] LightsIntersectingRect(RectangleF rect)
        {

            return
                _lights.FindAll(
                    l => l.LightArea.LightPosition + l.LightArea.LightAreaSize / 2 > new Vector2D(rect.Left, rect.Top)
                         &&
                         l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2 < new Vector2D(rect.Right, rect.Bottom))
                    .ToArray();
        }

        public ILight[] LightsIntersectingPoint(Vector2D point)
        {

            return
                _lights.FindAll(
                    l => l.LightArea.LightPosition + l.LightArea.LightAreaSize / 2 > point
                         &&
                         l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2 < point)
                    .ToArray();
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

        public void RecalculateLightsInView(Vector2D point)
        {
            var lights = LightsIntersectingPoint(point);
            foreach(var l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public void RecalculateLightsInView(RectangleF rect)
        {
            var lights = LightsIntersectingRect(rect);
            foreach(var l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }
    }

}