using System;
using System.Runtime.InteropServices;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vector4d
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        public readonly double W;

        public Vector4d(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void Deconstruct(out double x, out double y, out double z, out double w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public static implicit operator Vector4d((double, double, double, double) tuple)
        {
            return new Vector4d(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }

        public static implicit operator Vector4d(Vector4 vector)
        {
            return new Vector4d(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
