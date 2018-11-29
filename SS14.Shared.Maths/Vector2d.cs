using System;
using System.Runtime.InteropServices;

namespace SS14.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vector2d
    {
        public readonly double X;
        public readonly double Y;

        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vector2d(Vector2 vector)
        {
            return new Vector2d(vector.X, vector.Y);
        }
    }
}
