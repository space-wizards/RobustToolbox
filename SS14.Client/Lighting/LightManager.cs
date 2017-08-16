using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Lighting;
using SS14.Shared;
using SS14.Shared.Maths;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Client.Lighting
{
    public class LightManager : ILightManager
    {
        private readonly List<Type> LightModes = new List<Type>();
        private readonly List<ILight> _lights = new List<ILight>();

        public LightManager()
        {
            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LightModes =
                assemblies.SelectMany(t => t.GetTypes()).Where(
                    p => typeof (LightMode).IsAssignableFrom(p) && !p.IsInterface).ToList();
        }

        #region ILightManager Members

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

            foreach (Type t in LightModes)
            {
                var temp = (LightMode) Activator.CreateInstance(t);
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
            if (!_lights.Contains(light))
                _lights.Add(light);
        }

        public void RemoveLight(ILight light)
        {
            if (_lights.Contains(light))
                _lights.Remove(light);
        }

        public ILight[] GetLights()
        {
            return _lights.ToArray();
        }

        public ILight[] lightsInRadius(Vector2 point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Position - point).LengthSquared) <= radius * radius).ToArray();
        }

        public ILight[] LightsIntersectingRect(Box2 rect)
        {
            return _lights
                .FindAll(l => Box2.FromDimensions(l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2, l.LightArea.LightAreaSize).Intersects(rect))
                .ToArray();
        }

        public ILight[] LightsIntersectingPoint(Vector2 point)
        {
            return _lights
                .FindAll(l => Box2.FromDimensions(l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2, l.LightArea.LightAreaSize).Contains(point))
                .ToArray();
        }

        public ILight CreateLight()
        {
            return new Light();
        }

        public void RecalculateLights()
        {
            foreach (ILight l in _lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public void RecalculateLightsInView(Vector2 point)
        {
            ILight[] lights = LightsIntersectingPoint(point);
            foreach (ILight l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public void RecalculateLightsInView(Box2 rect)
        {
            ILight[] lights = LightsIntersectingRect(rect);
            foreach (ILight l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        #endregion
    }
}
