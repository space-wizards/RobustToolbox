using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    public class LightManager : ILightManager
    {
        private readonly List<ILight> _lights = new List<ILight>();
        private readonly List<Type> LightModes = new List<Type>();

        public LightManager()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LightModes =
                assemblies.SelectMany(t => t.GetTypes()).Where(
                    p => typeof(LightMode).IsAssignableFrom(p) && !p.IsInterface).ToList();
        }

        public SFML.Graphics.Sprite LightMask { get; set; }

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

            foreach (var t in LightModes)
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

        public ILight[] LightsIntersectingRect(Box2 rect)
        {
            return _lights
                .FindAll(l => Box2.FromDimensions(l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2, l.LightArea.LightAreaSize).Intersects(rect))
                .ToArray();
        }

        public ILight CreateLight()
        {
            return new Light();
        }

        public void RecalculateLights()
        {
            foreach (var l in _lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public void RecalculateLightsInView(Box2 rect)
        {
            var lights = LightsIntersectingRect(rect);
            foreach (var l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public ILight[] lightsInRadius(Vector2 point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Position - point).LengthSquared) <= radius * radius).ToArray();
        }

        public ILight[] LightsIntersectingPoint(Vector2 point)
        {
            return _lights
                .FindAll(l => Box2.FromDimensions(l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2, l.LightArea.LightAreaSize).Contains(point))
                .ToArray();
        }

        public void RecalculateLightsInView(Vector2 point)
        {
            var lights = LightsIntersectingPoint(point);
            foreach (var l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }
    }
}
