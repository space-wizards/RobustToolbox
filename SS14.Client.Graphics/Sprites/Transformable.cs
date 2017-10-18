using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System;
using STransformable = SFML.Graphics.Transformable;

namespace SS14.Client.Graphics.Sprites
{
    public abstract class Transformable : IDisposable
    {
        public abstract STransformable SFMLTransformable { get; }

        public void Dispose() => SFMLTransformable.Dispose();

        public Vector2 Position
        {
            get => SFMLTransformable.Position.Convert();
            set => SFMLTransformable.Position = value.Convert();
        }

        public Vector2 Origin
        {
            get => SFMLTransformable.Origin.Convert();
            set => SFMLTransformable.Origin = value.Convert();
        }

        public Vector2 Scale
        {
            get => SFMLTransformable.Scale.Convert();
            set => SFMLTransformable.Scale = value.Convert();
        }

        public Angle Rotation
        {
            get => Angle.FromDegrees(SFMLTransformable.Rotation);
            set => SFMLTransformable.Rotation = (float)value.Degrees;
        }

    }
}
