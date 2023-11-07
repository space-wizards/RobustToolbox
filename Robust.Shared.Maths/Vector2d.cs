using System;
using System.Numerics;
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

        public static explicit operator Vector2(Vector2d vector)
        {
            return new Vector2((float) vector.X, (float) vector.Y);
        }

        public static Vector2d operator +(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X + b.X, a.Y + b.Y);
        }
    }
}
