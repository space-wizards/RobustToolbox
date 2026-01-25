using System;
using System.IO;
using JetBrains.Annotations;
using NetSerializer;

namespace Robust.Shared.Serialization;

/// <summary>
/// "Safer" read primitives as an alternative to <see cref="Primitives"/>.
/// </summary>
internal static class SafePrimitives
{
    /// <summary>
    /// Read a float value from the stream, flushing NaNs to zero.
    /// </summary>
    [UsedImplicitly]
    public static void ReadPrimitive(Stream stream, out float value)
    {
        Primitives.ReadPrimitive(stream, out float readFloat);

        value = float.IsNaN(readFloat) ? 0 : readFloat;
    }

    /// <summary>
    /// Read a double value from the stream, flushing NaNs to zero.
    /// </summary>
    [UsedImplicitly]
    public static void ReadPrimitive(Stream stream, out double value)
    {
        Primitives.ReadPrimitive(stream, out double readDouble);

        value = double.IsNaN(readDouble) ? 0 : readDouble;
    }

    /// <summary>
    /// Read a double value from the stream, flushing NaNs to zero.
    /// </summary>
    [UsedImplicitly]
    public static void ReadPrimitive(Stream stream, out Half value)
    {
        Primitives.ReadPrimitive(stream, out Half readDouble);

        value = Half.IsNaN(readDouble) ? Half.Zero : readDouble;
    }
}
