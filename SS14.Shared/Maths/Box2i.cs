using OpenTK;
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
        public int Width => Left - Right;
        public int Height => Top - Bottom;

        public Box2i(Vector2i TopLeft, Vector2i BottomRight)
        {
            Left = TopLeft.X;
            Top = TopLeft.Y;
            Bottom = BottomRight.Y;
            Right = BottomRight.X;
        }

        public Box2i(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
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
    }
}

