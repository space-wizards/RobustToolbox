using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4d
    {
        public double X;
        public double Y;
        public double Z;
        public double W;

        public Vector4d(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public readonly void Deconstruct(out double x, out double y, out double z, out double w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public static implicit operator Vector4d((double, double, double, double) tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }

        public static implicit operator Vector4d(Vector4 vector)
        {
            return new(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
