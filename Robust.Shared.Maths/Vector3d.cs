using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3d
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly void Deconstruct(out double x, out double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public static implicit operator Vector3d((double, double, double) tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator Vector3d(Vector3 vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }
    }
}
