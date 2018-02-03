using System;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Shared.IoC;

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

        public Eye()
        {
            GodotCamera = new Godot.Camera2D()
            {
                DragMarginHEnabled = false,
                DragMarginVEnabled = false,
            };
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

            GodotCamera.QueueFree();
            GodotCamera.Dispose();
            GodotCamera = null;
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Eye));
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
