/// Shitty vector2 class implemented from http://www.codeproject.com/KB/recipes/VectorType.aspx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct Vector2
{
    private double x;
    private double y;

    public double X
    {
        get { return x; }
        set { x = value; }
    }

    public double Y
    {
        get { return y; }
        set { y = value; }
    }

    public double Magnitude
    {
        get
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", value,
                  NEGATIVE_MAGNITUDE);
            }

            if (this.Magnitude == 0)
            { throw new ArgumentException(ORIGIN_VECTOR_MAGNITUDE, "this"); }

            this = this * (value / Magnitude);
        }
    }

    private const string NEGATIVE_MAGNITUDE =
  "The magnitude of a Vector must be a positive value, (i.e. greater than 0)";

    private const string ORIGIN_VECTOR_MAGNITUDE =
       "Cannot change the magnitude of Vector(0,0,0)";

    public Vector2(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2 operator +(Vector2 v1, Vector2 v2)
    {
        return
        (
            new Vector2(v1.X + v2.X, v1.Y + v2.Y)
        );
    }

    public static Vector2 operator -(Vector2 v1, Vector2 v2)
    {
        return (new Vector2(v1.X - v2.X, v1.Y - v2.Y));
    }

    public static bool operator <(Vector2 v1, Vector2 v2)
    {
        return v1.Magnitude < v2.Magnitude;
    }

    public static bool operator <=(Vector2 v1, Vector2 v2)
    {
        return v1.Magnitude <= v2.Magnitude;
    }

    public static bool operator >(Vector2 v1, Vector2 v2)
    {
        return v1.Magnitude > v2.Magnitude;
    }

    public static bool operator >=(Vector2 v1, Vector2 v2)
    {
        return v1.Magnitude >= v2.Magnitude;
    }

    public static bool operator ==(Vector2 v1, Vector2 v2)
    {
        return
        (
           (v1.X == v2.X) &&
           (v1.Y == v2.Y)
        );
    }

    public static bool operator !=(Vector2 v1, Vector2 v2)
    {
        return !(v1 == v2);
    }

    public static Vector2 operator /(Vector2 v1, double s2)
    {
        return
        (
           new Vector2
           (
              v1.X / s2,
              v1.Y / s2
           )
        );
    }

    public static Vector2 operator *(Vector2 v1, double s2)
    {
        return
        (
           new Vector2
           (
              v1.X * s2,
              v1.Y * s2
           )
        );
    }

    public static Vector2 operator *(double s1, Vector2 v2)
    {
        return v2 * s1;
    }

    public static bool IsUnitVector(Vector2 v1)
    {
        return v1.Magnitude == 1;
    }

    public bool IsUnitVector()
    {
        return IsUnitVector(this);
    }

    public static Vector2 Normalize(Vector2 v1)
    {
        // Check for divide by zero errors

        if (v1.Magnitude == 0)
        {
            throw new DivideByZeroException(NORMALIZE_0);
        }
        else
        {
            // find the inverse of the vectors magnitude

            double inverse = 1 / v1.Magnitude;
            return
            (
               new Vector2
               (
                // multiply each component by the inverse of the magnitude

                  v1.X * inverse,
                  v1.Y * inverse
               )
            );
        }
    }

    public void Normalize()
    {
        this = Normalize(this);
    }

    private const string NORMALIZE_0 = "Can not normalize a vector when" +
        "it's magnitude is zero";

    public static double Distance(Vector2 v1, Vector2 v2)
    {
        return
        (
           Math.Sqrt
           (
               (v1.X - v2.X) * (v1.X - v2.X) +
               (v1.Y - v2.Y) * (v1.Y - v2.Y)
           )
        );
    }

    public double Distance(Vector2 other)
    {
        return Distance(this, other);
    }

    public static double DotProduct(Vector2 v1, Vector2 v2)
    {
        return
        (
           v1.X * v2.X +
           v1.Y * v2.Y
        );
    }

    public double DotProduct(Vector2 other)
    {
        return DotProduct(this, other);
    }

    public static double Angle(Vector2 v1, Vector2 v2)
    {
        if(v1.Magnitude == 0 || v2.Magnitude == 0)
            throw new ArgumentException(ZERO_VECTOR_ANGLE, "this");
        return
        (
           Math.Acos
           (
              Normalize(v1).DotProduct(Normalize(v2))
           )
        );
    }

    private const string ZERO_VECTOR_ANGLE = "Cannot find an angle from a zero vector.";

    public double Angle(Vector2 other)
    {
        return Angle(this, other);
    }
}