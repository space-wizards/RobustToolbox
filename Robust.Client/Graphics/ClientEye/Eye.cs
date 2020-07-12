using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

#nullable enable

namespace Robust.Client.Graphics.ClientEye
{
    /// <inheritdoc />
    public class Eye : IEye
    {
        private Vector2 _scale = Vector2.One;
        private Angle _rotation = Angle.Zero;
        private MapCoordinates _coords;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool DrawFov { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual MapCoordinates Position
        {
            get => _coords;
            internal set => _coords = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Zoom
        {
            get => new Vector2(1 / _scale.X, 1 / _scale.Y);
            set => _scale = new Vector2(1 / value.X, 1 / value.Y);
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Scale
        {
            get => _scale;
            set => _scale = value;
        }

        /// <inheritdoc />
        public void GetViewMatrix(out Matrix3 viewMatrix)
        {
            //TODO: Something is funky with this, matrices are being multiplied backwards, Position is negative
            //Should this be a normal transform that is inverted?
            var scaleMat = Matrix3.CreateScale(_scale.X, _scale.Y);
            var rotMat = Matrix3.CreateRotation(_rotation);
            var transMat = Matrix3.CreateTranslation(-_coords.Position);

            viewMatrix = transMat * rotMat * scaleMat;
        }

        /// <inheritdoc />
        public void GetViewMatrixInv(out Matrix3 viewMatrixInv)
        {
            GetViewMatrix(out var viewMatrix);
            viewMatrixInv = Matrix3.Invert(viewMatrix);
        }
    }
}
