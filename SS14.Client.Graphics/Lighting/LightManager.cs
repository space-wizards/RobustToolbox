using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SS14.Shared;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Graphics.Sprites;
using System.Reflection;
using SS14.Shared.Log;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Map;

namespace SS14.Client.Graphics.Lighting
{
    public class LightManager : ILightManager
    {
        private readonly List<ILight> _lights = new List<ILight>();
        private readonly List<Type> LightModes = new List<Type>();

        [Dependency]
        private readonly IReflectionManager reflectionManager;

        public void Initialize()
        {
            LightModes.AddRange(reflectionManager.GetAllChildren<LightMode>());
        }

        public Sprite LightMask { get; set; }

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

        public ILight[] LightsIntersectingRect(MapId mapId, Box2 rect)
        {
            if(rect.IsEmpty())
                return new ILight[0];

            return _lights
                .FindAll(l => l.Coordinates.MapID == mapId &&
                Box2.FromDimensions(l.LightArea.LightPosition - l.LightArea.LightAreaSize / 2, l.LightArea.LightAreaSize).Intersects(rect))
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

        public void RecalculateLightsInView(MapId mapId, Box2 rect)
        {
            var lights = LightsIntersectingRect(mapId, rect);
            foreach (var l in lights)
            {
                l.LightArea.Calculated = false;
            }
        }

        public ILight[] lightsInRadius(Vector2 point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Coordinates.Position - point).LengthSquared) <= radius * radius).ToArray();
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
