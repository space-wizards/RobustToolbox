using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct UIBox2i : IEquatable<UIBox2i>, ISpanFormattable
{
    [FieldOffset(sizeof(int) * 0)] internal int _left;
    [FieldOffset(sizeof(int) * 1)] internal int _top;
    [FieldOffset(sizeof(int) * 2)] internal int _right;
    [FieldOffset(sizeof(int) * 3)] internal int _bottom;

    [FieldOffset(sizeof(int) * 0)] internal Vector2i _topLeft;
    [FieldOffset(sizeof(int) * 2)] internal Vector2i _bottomRight;

    public int Left
    {
        readonly get => _left;
        set
        {
            if (value > _right)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Left cannot be greater than Right.");

            _left = value;
        }
    }

    public int Top
    {
        readonly get => _top;
        set
        {
            if (value > _bottom)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Top cannot be greater than Bottom.");

            _top = value;
        }
    }

    public int Right
    {
        readonly get => _right;
        set
        {
            if (value < _left)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Right cannot be less than Left.");

            _right = value;
        }
    }

    public int Bottom
    {
        readonly get => _bottom;
        set
        {
            if (value < _top)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Bottom cannot be less than Top.");

            _bottom = value;
        }
    }

    public Vector2i TopLeft
    {
        readonly get => _topLeft;
        set
        {
            if (value.X > _right)
                throw new ArgumentOutOfRangeException(nameof(value), value, "TopLeft.X cannot be greater than Right.");

            if (value.Y > _bottom)
                throw new ArgumentOutOfRangeException(nameof(value), value, "TopLeft.Y cannot be greater than Bottom.");

            _topLeft = value;
        }
    }

    public Vector2i BottomRight
    {
        readonly get => _bottomRight;
        set
        {
            if (value.X < _left)
                throw new ArgumentOutOfRangeException(nameof(value), value, "BottomRight.X cannot be less than Left.");

            if (value.Y < _top)
                throw new ArgumentOutOfRangeException(nameof(value), value, "BottomRight.Y cannot be less than Top.");

            _bottomRight = value;
        }
    }

    public readonly Vector2i TopRight => new(Right, Top);

    public readonly Vector2i BottomLeft => new(Left, Bottom);

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public readonly Vector2i Size => new(Width, Height);

    public readonly Vector2 Center => new Vector2(_left + _right, _top + _bottom) / 2f;

    public UIBox2i(Vector2i topLeft, Vector2i bottomRight)
    {
        Unsafe.SkipInit(out this);

        _topLeft = topLeft;
        _bottomRight = bottomRight;
    }

    public UIBox2i(int left, int top, int right, int bottom)
    {
        Unsafe.SkipInit(out this);

        _left = left;
        _right = right;
        _top = top;
        _bottom = bottom;
    }

    public static UIBox2i FromDimensions(int left, int top, int width, int height)
    {
        return new UIBox2i(left, top, left + width, top + height);
    }

    public static UIBox2i FromDimensions(Vector2i position, Vector2i size)
    {
        return FromDimensions(position.X, position.Y, size.X, size.Y);
    }

    public readonly bool Contains(int x, int y)
    {
        return Contains(new Vector2i(x, y));
    }

    public readonly bool Contains(Vector2i point, bool closedRegion = true)
    {
        var xOk = closedRegion
            ? point.X >= Left ^ point.X > Right
            : point.X > Left ^ point.X >= Right;
        var yOk = closedRegion
            ? point.Y >= Top ^ point.Y > Bottom
            : point.Y > Top ^ point.Y >= Bottom;
        return xOk && yOk;
    }

    /// <summary>Returns a UIBox2 translated by the given amount.</summary>
    public readonly UIBox2i Translated(Vector2i point)
    {
        return new UIBox2i(Left + point.X, Top + point.Y, Right + point.X, Bottom + point.Y);
    }

    /// <summary>
    ///     Calculates the "intersection" of this and another box.
    ///     Basically, the smallest region that fits in both boxes.
    /// </summary>
    /// <param name="other">The box to calculate the intersection with.</param>
    /// <returns>
    ///     <c>null</c> if there is no intersection, otherwise the smallest region that fits in both boxes.
    /// </returns>
    public readonly UIBox2i? Intersection(in UIBox2i other)
    {
        if (!Intersects(other))
        {
            return null;
        }

        return new UIBox2i(
            Vector2i.ComponentMax(TopLeft, other.TopLeft),
            Vector2i.ComponentMin(BottomRight, other.BottomRight));
    }

    public readonly bool Intersects(in UIBox2i other)
    {
        return other._bottom >= _top
               && other._top <= _bottom
               && other._right >= _left
               && other._left <= _right;
    }

    // override object.Equals
    public readonly override bool Equals(object? obj)
    {
        if (obj is UIBox2i box)
        {
            return Equals(box);
        }

        return false;
    }

    public readonly bool Equals(UIBox2i other)
    {
        return other.Left == Left && other.Right == Right && other.Bottom == Bottom && other.Top == Top;
    }

    // override object.GetHashCode
    public readonly override int GetHashCode()
    {
        var code = Left.GetHashCode();
        code = (code * 929) ^ Right.GetHashCode();
        code = (code * 929) ^ Top.GetHashCode();
        code = (code * 929) ^ Bottom.GetHashCode();
        return code;
    }

    public static explicit operator UIBox2i(UIBox2 box)
    {
        return new UIBox2i((int) box.Left, (int) box.Top, (int) box.Right, (int) box.Bottom);
    }

    public static implicit operator UIBox2(UIBox2i box)
    {
        return new UIBox2(box.Left, box.Top, box.Right, box.Bottom);
    }

    public static bool operator ==(UIBox2i a, UIBox2i b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(UIBox2i a, UIBox2i b)
    {
        return !a.Equals(b);
    }

    public static UIBox2i operator +(UIBox2i box, (int lo, int to, int ro, int bo) offsets)
    {
        var (lo, to, ro, bo) = offsets;

        return new UIBox2i(box.Left + lo, box.Top + to, box.Right + ro, box.Bottom + bo);
    }

    public readonly override string ToString()
    {
        return $"({Left}, {Top}, {Right}, {Bottom})";
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return FormatHelpers.TryFormatInto(
            destination,
            out charsWritten,
            $"({Left}, {Top}, {Right}, {Bottom})");
    }
}
