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
        #if GODOT
        public Godot.Camera2D GodotCamera { get; private set; }
        #endif
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
            #if GODOT
            get => GodotCamera.Zoom.Convert();
            set => GodotCamera.Zoom = value.Convert();
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public MapId MapId { get; set; } = MapId.Nullspace;

        public Eye()
        {
            #if GODOT
            GodotCamera = new Godot.Camera2D()
            {
                DragMarginHEnabled = false,
                DragMarginVEnabled = false,
            };
            #endif
            eyeManager = IoCManager.Resolve<IEyeManager>();
        }

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;

            if (disposing)
            {
                Current = false;
                eyeManager = null;
            }

            #if GODOT
            GodotCamera.QueueFree();
            GodotCamera.Dispose();
            GodotCamera = null;
            #endif
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
