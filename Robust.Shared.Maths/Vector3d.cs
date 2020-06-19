using System;
using System.Runtime.InteropServices;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vector3d
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public static implicit operator Vector3d((double, double, double) tuple)
        {
            return new Vector3d(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator Vector3d(Vector3 vector)
        {
            return new Vector3d(vector.X, vector.Y, vector.Z);
        }
    }
}
