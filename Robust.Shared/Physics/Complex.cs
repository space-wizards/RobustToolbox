using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public struct Complex
    {
        public float Real;
        public float Imaginary;

        public static Complex One { get; } = new Complex(1, 0);

        public static Complex ImaginaryOne { get; } = new Complex(0, 1);

        public float Phase
        {
            get => MathF.Atan2(Imaginary, Real);
            set
            {
                if (value == 0)
                {
                    this = Complex.One;
                    return;
                }
                Real      = MathF.Cos(value);
                Imaginary = MathF.Sin(value);
            }
        }

        public float Magnitude => MathF.Round(MathF.Sqrt(MagnitudeSquared()));

        public Complex(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public static Complex FromAngle(float angle)
        {
            if (angle == 0)
                return One;

            return new Complex(
                MathF.Cos(angle),
                MathF.Sin(angle));
        }

        public void Conjugate()
        {
            Imaginary = -Imaginary;
        }

        public void Negate()
        {
            Real = -Real;
            Imaginary = -Imaginary;
        }

        public float MagnitudeSquared()
        {
            return (Real * Real) + (Imaginary * Imaginary);
        }

        public void Normalize()
        {
            var mag = Magnitude;
            Real = Real / mag;
            Imaginary = Imaginary / mag;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(Real, Imaginary);
        }

        public static Complex Multiply(Complex left, Complex right)
        {
            return new Complex( left.Real * right.Real - left.Imaginary * right.Imaginary,
                                left.Imaginary * right.Real + left.Real * right.Imaginary);
        }

        public static Complex Divide(Complex left, Complex right)
        {
            return new Complex( right.Real * left.Real + right.Imaginary * left.Imaginary,
                                right.Real * left.Imaginary - right.Imaginary * left.Real);
        }
        public static void Divide(Complex left, Complex right, out Complex result)
        {
            result = new Complex(right.Real * left.Real + right.Imaginary * left.Imaginary,
                                 right.Real * left.Imaginary - right.Imaginary * left.Real);
        }

        public static Vector2 Multiply(Vector2 left, Complex right)
        {
            return new Vector2(left.X * right.Real - left.Y * right.Imaginary,
                               left.Y * right.Real + left.X * right.Imaginary);
        }
        public static void Multiply(Vector2 left, Complex right, out Vector2 result)
        {
            result = new Vector2(left.X * right.Real - left.Y * right.Imaginary,
                                 left.Y * right.Real + left.X * right.Imaginary);
        }
        public static Vector2 Multiply(Vector2 left, ref Complex right)
        {
            return new Vector2(left.X * right.Real - left.Y * right.Imaginary,
                               left.Y * right.Real + left.X * right.Imaginary);
        }

        public static Vector2 Divide(Vector2 left, Complex right)
        {
            return new Vector2(left.X * right.Real + left.Y * right.Imaginary,
                               left.Y * right.Real - left.X * right.Imaginary);
        }

        public static Vector2 Divide(Vector2 left, ref Complex right)
        {
            return new Vector2(left.X * right.Real + left.Y * right.Imaginary,
                               left.Y * right.Real - left.X * right.Imaginary);
        }
        public static void Divide(Vector2 left, ref Complex right, out Vector2 result)
        {
            result = new Vector2(left.X * right.Real + left.Y * right.Imaginary,
                                 left.Y * right.Real - left.X * right.Imaginary);
        }

        public static Complex Conjugate(ref Complex value)
        {
            return new Complex(value.Real, -value.Imaginary);
        }

        public static Complex Negate(ref Complex value)
        {
            return new Complex(-value.Real, -value.Real);
        }

        public static Complex Normalize(ref Complex value)
        {
            var mag = value.Magnitude;
            return new Complex(value.Real / mag, -value.Imaginary / mag);
        }

        public override string ToString()
        {
            return $"{{Real: {Real} Imaginary: {Imaginary} Phase: {Phase} Magnitude: {Magnitude}}}";
        }
    }
}
