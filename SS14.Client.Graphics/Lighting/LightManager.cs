using System;
using System.Collections.Generic;
using SS14.Shared.Maths;
using SS14.Client.Graphics.Textures;
using SS14.Shared.Interfaces;
using SS14.Shared.Enums;
using SS14.Shared.Log;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Map;

namespace SS14.Client.Graphics.Lighting
{
    public class LightManager : ILightManager
    {
        private const string DefaultLightMaskTex = @"Textures/Unatlased/whitemask.png";

        private readonly List<ILight> _lights = new List<ILight>();
        private readonly List<Type> LightModes = new List<Type>();

        [Dependency]
        private readonly IReflectionManager reflectionManager;

        [Dependency]
        private readonly IResourceManager _resources;

        public void Initialize()
        {
            LightModes.AddRange(reflectionManager.GetAllChildren<LightMode>());

            if (_resources.TryContentFileRead(DefaultLightMaskTex, out var stream))
            {
                _defaultLightMask = new Texture(stream);
            }
            else
            {
                Logger.Error("Default light map texture could not be found!");
            }

        }

        private Texture _defaultLightMask;

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
                Box2.FromDimensions(l.LightPosition - l.LightMapSize / 2, l.LightMapSize).Intersects(rect))
                .ToArray();
        }

        public ILight CreateLight()
        {
            var light = new Light();
            light.Mask = _defaultLightMask;
            return light;
        }

        public void RecalculateLights()
        {
            foreach (var l in _lights)
            {
                l.Calculated = false;
            }
        }

        public void RecalculateLightsInView(MapId mapId, Box2 rect)
        {
            var lights = LightsIntersectingRect(mapId, rect);
            foreach (var l in lights)
            {
                l.Calculated = false;
            }
        }

        public ILight[] lightsInRadius(Vector2 point, float radius)
        {
            return _lights.FindAll(l => Math.Abs((l.Coordinates.Position - point).LengthSquared) <= radius * radius).ToArray();
        }

        public ILight[] LightsIntersectingPoint(Vector2 point)
        {
            return _lights
                .FindAll(l => Box2.FromDimensions(l.LightPosition - l.LightMapSize / 2, l.LightMapSize).Contains(point))
                .ToArray();
        }

        public void RecalculateLightsInView(Vector2 point)
        {
            var lights = LightsIntersectingPoint(point);
            foreach (var l in lights)
            {
                l.Calculated = false;
            }
        }
    }
}
