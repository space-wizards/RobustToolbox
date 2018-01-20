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

        private bool Deferred = false;

        private Godot.CanvasModulate canvasModulate;
        private Godot.Viewport rootViewport;
        private Godot.Viewport deferredViewport;
        private Godot.CanvasLayer deferredMaskLayer;
        private Godot.CanvasLayer deferredBackgroundLayer;
        private Godot.Sprite deferredMaskBackground;
        private Godot.Sprite deferredMaskSprite;
        private GodotGlue.GodotSignalSubscriber0 deferredSizeChangedSubscriber;

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

            if (Deferred)
            {
                rootViewport = sceneTreeHolder.SceneTree.Root;
                deferredViewport = new Godot.Viewport
                {
                    Name = "LightingViewport",
                    RenderTargetUpdateMode = Godot.Viewport.UpdateMode.Always,
                    RenderTargetVFlip = true,
                };
                deferredViewport.AddChild(canvasModulate);
                rootViewport.AddChild(deferredViewport);

                deferredBackgroundLayer = new Godot.CanvasLayer()
                {
                    Name = "DeferredMaskBackgroundLayer",
                    Layer = -1
                };
                deferredViewport.AddChild(deferredBackgroundLayer);

                var whiteTex = resourceCache.GetResource<TextureResource>(@"./Textures/Effects/Light/white.png");
                deferredMaskBackground = new Godot.Sprite()
                {
                    Name = "DeferredMaskBackground",
                    Texture = whiteTex.Texture.Texture,
                    Centered = false
                };
                deferredBackgroundLayer.AddChild(deferredMaskBackground);

                deferredSizeChangedSubscriber = new GodotGlue.GodotSignalSubscriber0();
                deferredSizeChangedSubscriber.Connect(rootViewport, "size_changed");
                deferredSizeChangedSubscriber.Signal += OnWindowSizeChanged;

                deferredMaskLayer = new Godot.CanvasLayer()
                {
                    Name = "LightingMaskLayer",
                    Layer = CanvasLayers.LAYER_DEFERRED_LIGHTING,
                };
                rootViewport.AddChild(deferredMaskLayer);

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
            }
        }

        public void Dispose()
        {
            if (canvasModulate != null)
            {
                canvasModulate.QueueFree();
                canvasModulate.Dispose();
                canvasModulate = null;
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
            light.UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            foreach (var light in lights)
            {
                light.UpdateEnabled();
            }
        }

        public void FrameProcess(FrameEventArgs args)
        {
            Logger.Debug(rootViewport.CanvasTransform.ToString());
            deferredViewport.CanvasTransform = rootViewport.CanvasTransform;

            foreach (var light in lights)
            {
                light.FrameProcess(args);
            }
        }
    }
}
