#nullable enable

using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Graphics
{
    /// <inheritdoc />
    [Virtual]
    public class Eye : IEye
    {
        private Vector2 _scale = Vector2.One / 2f;
        private Angle _rotation = Angle.Zero;
        private MapCoordinates _coords;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool DrawFov { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables]
        public bool DrawLight { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual MapCoordinates Position
        {
            get => _coords;
            set => _coords = value;
        }

        /// <summary>
        /// Eye offset, relative to the map, and not affected by <see cref="Rotation"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset { get; set; }

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
            get => new(1 / _scale.X, 1 / _scale.Y);
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
        public void GetViewMatrix(out Matrix3x2 viewMatrix, Vector2 renderScale)
        {
            viewMatrix = Matrix3Helpers.CreateInverseTransform(
                _coords.Position.X + Offset.X,
                _coords.Position.Y + Offset.Y,
                (float)-Rotation.Theta,
                1 / (_scale.X * renderScale.X),
                1 / (_scale.Y * renderScale.Y));
        }

        public void GetViewMatrixNoOffset(out Matrix3x2 viewMatrix, Vector2 renderScale)
        {
            viewMatrix = Matrix3Helpers.CreateInverseTransform(
                _coords.Position.X,
                _coords.Position.Y,
                (float)-Rotation.Theta,
                1 / (_scale.X * renderScale.X),
                1 / (_scale.Y * renderScale.Y));
        }

        /// <inheritdoc />
        public void GetViewMatrixInv(out Matrix3x2 viewMatrixInv, Vector2 renderScale)
        {
            viewMatrixInv = Matrix3Helpers.CreateTransform(
                _coords.Position.X + Offset.X,
                _coords.Position.Y + Offset.Y,
                (float)-Rotation.Theta,
                1 / (_scale.X * renderScale.X),
                1 / (_scale.Y * renderScale.Y));
        }
    }
}
