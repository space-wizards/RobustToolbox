using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System;
using SRectangleShape = SFML.Graphics.RectangleShape;
using SShape = SFML.Graphics.Shape;

namespace SS14.Client.Graphics.Sprites
{
    public class RectangleShape : Shape
    {
        public SRectangleShape SFMLRectangleShape { get; }
        public override SShape SFMLShape => SFMLRectangleShape;

        internal RectangleShape(SRectangleShape shape)
        {
            SFMLRectangleShape = shape;
        }

        public RectangleShape() : this(Vector2.Zero) {}
        public RectangleShape(Vector2 size) : this(new SRectangleShape(size.Convert())) {}
        public RectangleShape(RectangleShape shape) : this(new SRectangleShape(shape.SFMLRectangleShape))
        {
            Texture = shape.Texture;
        }
    }
}
