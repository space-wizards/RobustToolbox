using System;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.ClientEye
{
    public class Eye : IEye, IDisposable
    {
        protected IEyeManager eyeManager;
        public Godot.Camera2D GodotCamera { get; private set; }
        private bool disposed = false;

        public bool Current
        {
            get => eyeManager.CurrentEye == this;
            set
            {
                if (Current == value)
                {
                    return;
                }

                if (value)
                {
                    eyeManager.CurrentEye = this;
                }
                else
                {
                    eyeManager.CurrentEye = null;
                }
            }
        }


        public Vector2 Zoom
        {
            get => GameController.OnGodot ? GodotCamera.Zoom.Convert() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    GodotCamera.Zoom = value.Convert();
                }
            }
        }

        public MapId MapId { get; set; } = MapId.Nullspace;

        public Eye()
        {
            eyeManager = IoCManager.Resolve<IEyeManager>();
            if (GameController.OnGodot)
            {
                GodotCamera = new Godot.Camera2D()
                {
                    DragMarginHEnabled = false,
                    DragMarginVEnabled = false,
                };
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;

            if (disposing)
            {
                Current = false;
                eyeManager = null;
            }

            if (!GameController.OnGodot)
            {
                return;
            }
            GodotCamera.QueueFree();
            GodotCamera.Dispose();
            GodotCamera = null;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Eye()
        {
            Dispose(false);
        }
    }
}
