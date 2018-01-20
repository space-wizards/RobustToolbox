using System;
using System.Collections.Generic;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.Configuration;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.Log;
using SS14.Shared.Maths;

// Ok let me just quickly explain how deferred lighting works.
// So for some reason, Godot's Light2D has performance issues on some computers. Why? No clue. Anyways.
// So to fix this, we do "deferred lighting"!
// We have a config option to enable it because my shitty implementation doesn't look as good as normal Godot lighting.
// When enabled, each light is put into a separate Viewport. Said viewport contains the CanvasModulate and a white background.
// This means the viewport will have light rendering happen in it, onto a white canvas.
// The Viewport's output texture is then basically a lightmap we can blend over the rest of the game,
// which we do using a Sprite on its own canvas layer set to multiply blend in it CanvasMaterial.
// This performs better because we forgo the O(nlights + 1) drawing complexity of canvas items.
namespace SS14.Client.Graphics.Lighting
{
    partial class LightManager : ILightManager, IDisposable, IPostInjectInit
    {
        [Dependency]
        readonly ISceneTreeHolder sceneTreeHolder;
        [Dependency]
        readonly IConfigurationManager configManager;
        [Dependency]
        readonly IResourceCache resourceCache;

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
                UpdateEnabled();
            }
        }

        private List<Light> lights = new List<Light>();
        private List<Occluder> occluders = new List<Occluder>();

        private bool Deferred = false;

        private Godot.CanvasModulate canvasModulate;
        private Godot.Viewport rootViewport;
        private Godot.Viewport deferredViewport;
        private Godot.CanvasLayer deferredMaskLayer;
        private Godot.Sprite deferredMaskBackground;
        private Godot.Sprite deferredMaskSprite;
        private GodotGlue.GodotSignalSubscriber0 deferredSizeChangedSubscriber;

        private bool disposed = false;

        public void PostInject()
        {
            configManager.RegisterCVar("display.deferred_lighting", false, Shared.Configuration.CVar.ARCHIVE);
        }

        public void Initialize()
        {
            Deferred = configManager.GetCVar<bool>("display.deferred_lighting");
            canvasModulate = new Godot.CanvasModulate()
            {
                // Black
                Color = new Godot.Color(0.1f, 0.1f, 0.1f),
                Name = "LightingCanvasModulate"
            };
            rootViewport = sceneTreeHolder.SceneTree.Root;

            if (Deferred)
            {
                deferredViewport = new Godot.Viewport
                {
                    Name = "LightingViewport",
                    RenderTargetUpdateMode = Godot.Viewport.UpdateMode.Always,
                    RenderTargetVFlip = true,
                };
                deferredViewport.AddChild(canvasModulate);
                rootViewport.AddChild(deferredViewport);

                var whiteTex = resourceCache.GetResource<TextureResource>(@"./Textures/Effects/Light/white.png");
                deferredMaskBackground = new Godot.Sprite()
                {
                    Name = "DeferredMaskBackground",
                    Texture = whiteTex.Texture.Texture,
                    Centered = false,
                };
                deferredViewport.AddChild(deferredMaskBackground);

                deferredSizeChangedSubscriber = new GodotGlue.GodotSignalSubscriber0();
                deferredSizeChangedSubscriber.Connect(rootViewport, "size_changed");
                deferredSizeChangedSubscriber.Signal += OnWindowSizeChanged;

                deferredMaskLayer = new Godot.CanvasLayer()
                {
                    Name = "LightingMaskLayer",
                    Layer = CanvasLayers.LAYER_DEFERRED_LIGHTING,
                };
                rootViewport.AddChild(deferredMaskLayer);

                CreateDeferMaskSprite();
                OnWindowSizeChanged();
            }
            else
            {
                sceneTreeHolder.WorldRoot.AddChild(canvasModulate);
            }
        }

        private void OnWindowSizeChanged()
        {
            if (Deferred)
            {
                var size = sceneTreeHolder.SceneTree.Root.Size;
                deferredViewport.Size = size;
                deferredMaskBackground.Scale = size;
                // Needs to be re-created because otherwise it seems to not acknowledge the updated texture size?
                // Might be a better way to do this.
                CreateDeferMaskSprite();
            }
        }

        private void CreateDeferMaskSprite()
        {
            if (deferredMaskSprite != null)
            {
                deferredMaskSprite.QueueFree();
                deferredMaskSprite.Dispose();
            }

            var mat = new Godot.CanvasItemMaterial()
            {
                BlendMode = Godot.CanvasItemMaterial.BlendModeEnum.Mul
            };
            deferredMaskSprite = new Godot.Sprite
            {
                Name = "LightingDeferMask",
                Texture = deferredViewport.GetTexture(),
                Centered = false,
                Material = mat,
            };
            deferredMaskLayer.AddChild(deferredMaskSprite);
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

            if (Deferred)
            {
                deferredSizeChangedSubscriber.Disconnect(rootViewport, "size_changed");
                deferredSizeChangedSubscriber.Dispose();
                deferredSizeChangedSubscriber = null;
            }

            rootViewport = null;

            if (Deferred)
            {
                deferredViewport.QueueFree();
                deferredViewport.Dispose();
                deferredViewport = null;

                deferredMaskLayer.QueueFree();
                deferredMaskLayer.Dispose();
                deferredMaskLayer = null;

                // These are implicitly freed by the other free calls.
                deferredMaskBackground.Dispose();
                deferredMaskBackground = null;
                deferredMaskSprite.Dispose();
                deferredMaskSprite = null;
            }

            canvasModulate.QueueFree();
            canvasModulate.Dispose();
            canvasModulate = null;
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

        private void UpdateEnabled()
        {
            if (Deferred)
            {
                deferredMaskSprite.Visible = Enabled;
            }
            foreach (var light in lights)
            {
                light.UpdateEnabled();
            }
        }

        public void FrameProcess(FrameEventArgs args)
        {
            if (Deferred)
            {
                var transform = rootViewport.CanvasTransform;
                deferredViewport.CanvasTransform = transform;
                deferredMaskBackground.Transform = transform.Inverse();
                deferredMaskBackground.Scale = rootViewport.Size;
            }

            foreach (var light in lights)
            {
                light.FrameProcess(args);
            }

            foreach (var occluder in occluders)
            {
                occluder.FrameProcess(args);
            }
        }
    }
}
