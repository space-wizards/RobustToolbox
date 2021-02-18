using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2d
    {
        public double X;
        public double Y;

        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public readonly void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }

        public static implicit operator Vector2d((double, double) tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Vector2d(Vector2 vector)
        {
            return new(vector.X, vector.Y);
        }
    }
}
