using System;

namespace Robust.Shared.Maths;

/// <summary>
/// Marker type to indicate floating point values that should preserve NaNs across the network.
/// </summary>
/// <remarks>
/// Robust's network serializer may be configured to flush NaN float values to 0,
/// to avoid exploits from lacking input validation. Even if this feature is enabled,
/// NaN values passed in this type are still untouched.
/// </remarks>
/// <param name="Value">The actual inner floating point value</param>
/// <seealso cref="System.Half"/>
public readonly record struct UnsafeHalf(Half Value)
{
    public static implicit operator Half(UnsafeHalf f) => f.Value;
    public static implicit operator UnsafeHalf(Half f) => new(f);
}

/// <summary>
/// Marker type to indicate floating point values that should preserve NaNs across the network.
/// </summary>
/// <remarks>
/// Robust's network serializer may be configured to flush NaN float values to 0,
/// to avoid exploits from lacking input validation. Even if this feature is enabled,
/// NaN values passed in this type are still untouched.
/// </remarks>
/// <param name="Value">The actual inner floating point value</param>
/// <seealso cref="System.Single"/>
public readonly record struct UnsafeFloat(float Value)
{
    public static implicit operator float(UnsafeFloat f) => f.Value;
    public static implicit operator UnsafeFloat(float f) => new(f);
}

/// <summary>
/// Marker type to indicate floating point values that should preserve NaNs across the network.
/// </summary>
/// <remarks>
/// Robust's network serializer may be configured to flush NaN float values to 0,
/// to avoid exploits from lacking input validation. Even if this feature is enabled,
/// NaN values passed in this type are still untouched.
/// </remarks>
/// <param name="Value">The actual inner floating point value</param>
/// <seealso cref="System.Double"/>
public readonly record struct UnsafeDouble(double Value)
{
    public static implicit operator double(UnsafeDouble f) => f.Value;
    public static implicit operator UnsafeDouble(double f) => new(f);
    public static implicit operator UnsafeDouble(float f) => new(f);
    public static implicit operator UnsafeDouble(UnsafeFloat f) => new(f);
}
