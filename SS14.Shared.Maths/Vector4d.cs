using System;
using System.Runtime.InteropServices;

namespace SS14.Shared.Maths
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

        public static implicit operator Vector4d(Vector4 vector)
        {
            return new Vector4d(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
