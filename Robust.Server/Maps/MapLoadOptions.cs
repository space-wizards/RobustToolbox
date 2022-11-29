using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Server.Maps
{
    [PublicAPI]
    public sealed class MapLoadOptions
    {
        /// <summary>
        ///     If true, UID components will be created for loaded entities
        ///     to maintain consistency upon subsequent savings.
        /// </summary>
        public bool StoreMapUids { get; set; }

        /// <summary>
        ///     Offset to apply to the loaded objects.
        /// </summary>
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                TransformMatrix = Matrix3.CreateTransform(value, Rotation);
                _offset = value;
            }
        }

        private Vector2 _offset = Vector2.Zero;

        /// <summary>
        ///     Rotation to apply to the loaded objects as a collective, around 0, 0.
        /// </summary>
        /// <remarks>Setting this overrides </remarks>
        public Angle Rotation
        {
            get => _rotation;
            set
            {
                TransformMatrix = Matrix3.CreateTransform(Offset, value);
                _rotation = value;
            }
        }

        private Angle _rotation = Angle.Zero;

        public Matrix3 TransformMatrix { get; set; } = Matrix3.Identity;

        /// <summary>
        /// If there is a map entity serialized should we also load it.
        /// This should be set to false if you want to load a map file onto an existing map.
        /// </summary>
        public bool LoadMap { get; set; } = true;
    }
}
