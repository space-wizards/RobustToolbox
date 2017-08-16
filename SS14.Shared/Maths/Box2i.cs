using OpenTK;
using SFML.Graphics;
using SS14.Shared.Utility;
using System;

namespace SS14.Shared.Maths
{
    [Serializable]
    public struct Box2i : IEquatable<Box2i>
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Top;
        public readonly int Bottom;

        public Vector2i BottomRight => new Vector2i(Right, Bottom);
        public Vector2i TopLeft => new Vector2i(Left, Top);
        public int Width => Math.Abs(Right - Left);
        public int Height => Math.Abs(Top - Bottom);

        public Box2i(Vector2i TopLeft, Vector2i BottomRight)
        {
            Left = TopLeft.X;
            Top = TopLeft.Y;
            Bottom = BottomRight.Y;
            Right = BottomRight.X;
        }

        public Box2i(int left, int top, int right, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static Box2i FromDimensions(int left, int top, int width, int height)
        {
            return new Box2i(left, top, left + width, top + height);
        }

        public static Box2i FromDimensions(Vector2i position, Vector2i size)
        {
            return FromDimensions(position.X, position.Y, size.X, size.Y);
        }

        public bool Contains(Vector2i point)
        {
            return Contains(point, true);
        }

        public bool Contains(int x, int y)
        {
            return Contains(new Vector2i(x, y));
        }

        public bool Contains(Vector2i point, bool closedRegion)
        {
            bool xOK = (closedRegion == Left <= Right) ?
                (point.X >= Left != point.X > Right) :
                (point.X > Left != point.X >= Right);

            bool yOK = (closedRegion == Top <= Bottom) ?
                (point.Y >= Top != point.Y > Bottom) :
                (point.Y > Top != point.Y >= Bottom);

            return xOK && yOK;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj is Box2i box)
            {
                return Equals(box);
            }

            return false;
        }

        public bool Equals(Box2i box)
        {
            return box.Left == Left && box.Right == Right && box.Bottom == Bottom && box.Top == Top;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            var code = Left.GetHashCode();
            code = (code * 929) ^ Right.GetHashCode();
            code = (code * 929) ^ Top.GetHashCode();
            code = (code * 929) ^ Bottom.GetHashCode();
            return code;
        }

        public static implicit operator Box2i(IntRect rect)
        {
            return Box2i.FromDimensions(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static implicit operator IntRect(Box2i box)
        {
            return new IntRect(box.Left, box.Top, box.Width, box.Height);
        }
    }
}

