using System;
using System.Collections.Generic;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Configuration;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

// Ok let me just quickly explain how deferred lighting works.
// So for some reason, Godot's Light2D has performance issues on some computers. Why? No clue. Anyways.
// So to fix this, we do "deferred lighting"!
// We have a config option to enable it because my shitty implementation doesn't look as good as normal Godot lighting.
// When enabled, each light is put into a separate Viewport. Said viewport contains the CanvasModulate and a white background.
// This means the viewport will have light rendering happen in it, onto a white canvas.
// The Viewport's output texture is then basically a lightmap we can blend over the rest of the game,
// which we do using a Sprite on its own canvas layer set to multiply blend in it CanvasMaterial.
// This performs better because we forgo the O(nlights + 1) drawing complexity of canvas items.
namespace Robust.Client.Graphics.Lighting
{
    public sealed partial class LightManager : ILightManager, IDisposable, IPostInjectInit
    {
        [Dependency] readonly IConfigurationManager configManager;
        [Dependency] readonly IResourceCache resourceCache;

        private bool enabled = true;

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value == Enabled)
                {
                    return;
                }

                enabled = value;
            }
        }

        private List<Light> lights = new List<Light>();
        private List<Occluder> occluders = new List<Occluder>();

        private LightingSystem System = LightingSystem.Normal;

        private bool disposed = false;

        public void PostInject()
        {
            configManager.RegisterCVar("display.lighting_system", LightingSystem.Normal,
                Shared.Configuration.CVar.ARCHIVE);
        }

        public void Initialize()
        {
            System = configManager.GetCVar<LightingSystem>("display.lighting_system");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            var localLights = new List<ILight>(lights);
            foreach (var light in localLights)
            {
                light.Dispose();
            }
        }

        public ILight MakeLight()
        {
            var light = new Light(this);
            lights.Add(light);
            light.UpdateEnabled();
            return light;
        }

        private void RemoveLight(Light light)
        {
            // TODO: This removal is O(n) because it's a regualar list,
            // and might become a performance issue.
            // Use some smarter datastructure maybe?
            lights.Remove(light);
        }

        public IOccluder MakeOccluder()
        {
            var occluder = new Occluder(this);
            occluders.Add(occluder);
            return occluder;
        }

        private void RemoveOccluder(Occluder occluder)
        {
            // TODO: This removal is O(n) because it's a regualar list,
            // and might become a performance issue.
            // Use some smarter datastructure maybe?
            occluders.Remove(occluder);
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
            foreach (var light in lights)
            {
                light.FrameProcess(args);
            }

            foreach (var occluder in occluders)
            {
                occluder.FrameProcess(args);
            }
        }

        public enum LightingSystem
        {
            Normal = 0,
            Deferred = 1,
            Disabled = 2,
        }
    }
}
