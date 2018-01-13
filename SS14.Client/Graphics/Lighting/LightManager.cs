using System;
using System.Collections.Generic;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Client.Graphics.Lighting
{
    class LightManager : ILightManager, IDisposable
    {
        [Dependency]
        readonly ISceneTreeHolder sceneTreeHolder;

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

        private Godot.CanvasModulate canvasModulate;
        private List<ILight> lights = new List<ILight>();

        public void Initialize()
        {
            canvasModulate = new Godot.CanvasModulate()
            {
                // Black
                Color = new Godot.Color(0.1f, 0.1f, 0.1f)
            };
            canvasModulate.SetName("LightManager");
            sceneTreeHolder.WorldRoot.AddChild(canvasModulate);
        }

        public void Dispose()
        {
            if (canvasModulate != null)
            {
                canvasModulate.QueueFree();
                canvasModulate.Dispose();
                canvasModulate = null;
            }
        }

        public void AddLight(ILight light)
        {
            lights.Add(light);
            light.UpdateEnabled();
        }


        public void RemoveLight(ILight light)
        {
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
    }
}
