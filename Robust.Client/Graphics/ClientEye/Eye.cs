using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.ClientEye
{
    /// <inheritdoc />
    public class Eye : IEye
    {
        //TODO: This is literally a Transformation matrix, might as well add Rotation to it as well.

        /// <inheritdoc />
        public Vector2 Zoom { get; set; } = Vector2.One;

        /// <inheritdoc />
        public virtual MapCoordinates Position { get; internal set; }
        
        /// <inheritdoc />
        public Matrix3 GetViewMatrix()
        {
            var matrix = Matrix3.Identity;
            matrix.R0C0 = 1 / Zoom.X;
            matrix.R1C1 = 1 / Zoom.Y;
            matrix.R0C2 = -Position.X / Zoom.X;
            matrix.R1C2 = -Position.Y / Zoom.Y;
            return matrix;
        }

        public bool Is3D { get; } = true;
    }
}
