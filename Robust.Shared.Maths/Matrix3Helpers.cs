using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;


namespace Robust.Shared.Maths;

public static class Matrix3Helpers
{
    public static bool EqualsApprox(this Matrix3x2 a, Matrix3x2 b, float tolerance = 1e-6f)
    {
        return
            Math.Abs(a.M11 - b.M11) <= tolerance &&
            Math.Abs(a.M12 - b.M12) <= tolerance &&
            Math.Abs(a.M21 - b.M21) <= tolerance &&
            Math.Abs(a.M22 - b.M22) <= tolerance &&
            Math.Abs(a.M31 - b.M31) <= tolerance &&
            Math.Abs(a.M32 - b.M32) <= tolerance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsApprox(this Matrix3x2 a, Matrix3x2 b, double tolerance)
    {
        return a.EqualsApprox(b, (float) tolerance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box2Rotated TransformBounds(this Matrix3x2 refFromBox, Box2Rotated box)
    {
        var matty = Matrix3x2.Multiply(refFromBox, box.Transform);
        return new Box2Rotated(Vector2.Transform(box.BottomLeft, matty), Vector2.Transform(box.TopRight, matty));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box2 TransformBox(this Matrix3x2 refFromBox, Box2Rotated box)
    {
        return (box.Transform * refFromBox).TransformBox(box.Box);
    }

    public static Box2 TransformBox(this Matrix3x2 refFromBox, in Box2 box)
    {
        // Do transformation on all 4 corners of the box at once.
        // Then min/max the results to get the new AABB.

        var boxVec = Unsafe.As<Box2, Vector128<float>>(ref Unsafe.AsRef(in box));

        // Convert box into list of X and Y values for each of the 4 corners
        var allX = Vector128.Shuffle(boxVec, Vector128.Create(0, 0, 2, 2));
        var allY = Vector128.Shuffle(boxVec, Vector128.Create(1, 3, 3, 1));

        // Transform coordinates
        var modX = allX * Vector128.Create(refFromBox.M11);
        var modY = allX * Vector128.Create(refFromBox.M12);
        modX += allY * Vector128.Create(refFromBox.M21);
        modY += allY * Vector128.Create(refFromBox.M22);
        modX += Vector128.Create(refFromBox.M31);
        modY += Vector128.Create(refFromBox.M32);

        // Get bounding box by finding the min and max X and Y values.
        var l = SimdHelpers.MinHorizontal128(modX);
        var b = SimdHelpers.MinHorizontal128(modY);
        var r = SimdHelpers.MaxHorizontal128(modX);
        var t = SimdHelpers.MaxHorizontal128(modY);

        var lbrt = SimdHelpers.MergeRows128(l, b, r, t);
        return Unsafe.As<Vector128<float>, Box2>(ref lbrt);
    }

    /// <summary>
    /// Gets the rotation of the Matrix. Will have some precision loss.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Angle Rotation(this Matrix3x2 t)
    {
        return new Angle(Math.Atan2(t.M12, t.M11));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateTransform(float posX, float posY, double angle, float scaleX = 1, float scaleY = 1)
    {
        // returns a matrix that is equivalent to returning CreateScale(scale) * CreateRotation(angle) * CreateTranslation(posX, posY)

        var sin = (float) Math.Sin(angle);
        var cos = (float) Math.Cos(angle);

        return new Matrix3x2
        {
            M11 = cos * scaleX,
            M21 = -sin * scaleY,
            M31 = posX,
            M12 = sin * scaleX,
            M22 = cos * scaleY,
            M32 = posY,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateTransform(in Vector2 position, in Angle angle)
    {
        // Rounding moment
        return angle.Theta switch
        {
            -Math.PI / 2 => new Matrix3x2(0f, -1f, 1, 0, position.X, position.Y),
            Math.PI / 2 => new Matrix3x2(0f, 1f, -1f, 0f, position.X, position.Y),
            Math.PI => new Matrix3x2(-1f, 0f, 0f, -1f, position.X, position.Y),
            _ => CreateTransform(position.X, position.Y, (float) angle.Theta)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateTransform(in Vector2 position, in Angle angle, in Vector2 scale)
    {
        return CreateTransform(position.X, position.Y, (float)angle.Theta, scale.X, scale.Y);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateInverseTransform(float posX, float posY, double angle, float scaleX = 1, float scaleY = 1)
    {
        // returns a matrix that is equivalent to returning CreateTranslation(-posX, -posY) * CreateRotation(-angle) * CreateScale(1/scaleX, 1/scaleY)

        var sin = (float) Math.Sin(angle);
        var cos = (float) Math.Cos(angle);

        return new Matrix3x2
        {
            M11 = cos / scaleX,
            M21 = sin / scaleX,
            M31 = - (posX * cos + posY * sin) / scaleX,
            M12 = -sin / scaleY,
            M22 = cos / scaleY,
            M32 = (posX * sin - posY * cos) / scaleY,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateInverseTransform(in Vector2 position, in Angle angle)
    {
        return CreateInverseTransform(position.X, position.Y, (float)angle.Theta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateInverseTransform(in Vector2 position, in Angle angle, in Vector2 scale)
    {
        return CreateInverseTransform(position.X, position.Y, (float)angle.Theta, scale.X, scale.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateTranslation(float x, float y)
    {
        return new Matrix3x2 {
            M11 = 1,
            M12 = 0,
            M21 = 0,
            M22 = 1,
            M31 = x,
            M32 = y
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateTranslation(Vector2 vector)
    {
        return CreateTranslation(vector.X, vector.Y);
    }

    public static Matrix3x2 CreateRotation(double angle)
    {
        var cos = (float) Math.Cos(angle);
        var sin = (float) Math.Sin(angle);
        return new Matrix3x2 {
            M11 = cos,
            M12 = sin,
            M21 = -sin,
            M22 = cos,
            M31 = 0,
            M32 = 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateScale(float x, float y)
    {
        return new Matrix3x2 {
            M11 = x,
            M12 = 0,
            M21 = 0,
            M22 = y,
            M31 = 0,
            M32 = 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 CreateScale(in Vector2 scale)
    {
        return CreateScale(scale.X, scale.Y);
    }
}
