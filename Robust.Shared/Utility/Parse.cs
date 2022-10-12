using System;
using System.Globalization;

namespace Robust.Shared.Utility;

/// <summary>
/// Helpers for parsing culture-invariant data.
/// </summary>
/// <remarks>
/// APIs like <see cref="System.Int32.TryParse(string, out int)"/> are culture sensitive by default,
/// and making them not culture sensitive is extremely verbose.
/// These helpers are culture insensitive without requiring you to write a whole shakespeare novel
/// for the privilege of having code that isn't gonna break for French people.</remarks>
public static class Parse
{
    // INT16

    public static bool TryInt16(ReadOnlySpan<char> text, out short result)
    {
        return TryInt16(text, NumberStyles.Integer, out result);
    }

    public static bool TryInt16(ReadOnlySpan<char> text, NumberStyles style, out short result)
    {
        return short.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static short Int16(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return short.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // UINT16

    public static bool TryUInt16(ReadOnlySpan<char> text, out ushort result)
    {
        return TryUInt16(text, NumberStyles.Integer, out result);
    }

    public static bool TryUInt16(ReadOnlySpan<char> text, NumberStyles style, out ushort result)
    {
        return ushort.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static ushort UInt16(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return ushort.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // INT32

    public static bool TryInt32(ReadOnlySpan<char> text, out int result)
    {
        return TryInt32(text, NumberStyles.Integer, out result);
    }

    public static bool TryInt32(ReadOnlySpan<char> text, NumberStyles style, out int result)
    {
        return int.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static int Int32(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return int.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // UINT32

    public static bool TryUInt32(ReadOnlySpan<char> text, out uint result)
    {
        return TryUInt32(text, NumberStyles.Integer, out result);
    }

    public static bool TryUInt32(ReadOnlySpan<char> text, NumberStyles style, out uint result)
    {
        return uint.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static uint UInt32(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return uint.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // INT64

    public static bool TryInt64(ReadOnlySpan<char> text, out long result)
    {
        return TryInt64(text, NumberStyles.Integer, out result);
    }

    public static bool TryInt64(ReadOnlySpan<char> text, NumberStyles style, out long result)
    {
        return long.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static long Int64(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return long.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // UINT64

    public static bool TryUInt64(ReadOnlySpan<char> text, out ulong result)
    {
        return TryUInt64(text, NumberStyles.Integer, out result);
    }

    public static bool TryUInt64(ReadOnlySpan<char> text, NumberStyles style, out ulong result)
    {
        return ulong.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static ulong UInt64(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Integer)
    {
        return ulong.Parse(text, style, CultureInfo.InvariantCulture);
    }


    // FLOAT

    public static bool TryFloat(ReadOnlySpan<char> text, out float result)
    {
        return TryFloat(text, NumberStyles.Float, out result);
    }

    public static bool TryFloat(ReadOnlySpan<char> text, NumberStyles style, out float result)
    {
        return float.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static float Float(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Float)
    {
        return float.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // DOUBLE

    public static bool TryDouble(ReadOnlySpan<char> text, out double result)
    {
        return TryDouble(text, NumberStyles.Float, out result);
    }

    public static bool TryDouble(ReadOnlySpan<char> text, NumberStyles style, out double result)
    {
        return double.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static double Double(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Float)
    {
        return double.Parse(text, style, CultureInfo.InvariantCulture);
    }

    // DECIMAL

    public static bool TryDecimal(ReadOnlySpan<char> text, out decimal result)
    {
        return TryDecimal(text, NumberStyles.Float, out result);
    }

    public static bool TryDecimal(ReadOnlySpan<char> text, NumberStyles style, out decimal result)
    {
        return decimal.TryParse(text, style, CultureInfo.InvariantCulture, out result);
    }

    public static decimal Decimal(ReadOnlySpan<char> text, NumberStyles style = NumberStyles.Float)
    {
        return decimal.Parse(text, style, CultureInfo.InvariantCulture);
    }
}
