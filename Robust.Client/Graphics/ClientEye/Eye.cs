using System;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.ClientEye
{
    public class Eye : IEye, IDisposable
    {
        protected IEyeManager eyeManager;
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

        public Vector2 Zoom { get; set; } = Vector2.One;

        private MapCoordinates _position;

        public virtual MapCoordinates Position
        {
            get => _position;
            internal set => _position = value;
        }

        public MapId MapId => _position.MapId;

        public Eye()
        {
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

    public static class EyeExtensions
    {
        public static Matrix3 GetMatrix(this IEye eye)
        {
            var matrix = Matrix3.Identity;
            matrix.R0C0 = 1 / eye.Zoom.X;
            matrix.R1C1 = 1 / eye.Zoom.Y;
            matrix.R0C2 = -eye.Position.X / eye.Zoom.X;
            matrix.R1C2 = -eye.Position.Y / eye.Zoom.Y;
            return matrix;
        }
    }
}
