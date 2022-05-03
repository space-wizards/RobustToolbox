using System.Buffers;
using System.Numerics;

namespace Robust.Shared.Utility;

/// <summary>
/// Helpers for dealing with buffer-like arrays.
/// </summary>
public static class BufferHelpers
{
    /// <summary>
    /// Resize the given buffer to the next power of two that fits the needed size.
    /// The contents of the buffer are NOT preserved if resized.
    /// </summary>
    public static void EnsureBuffer<T>(ref T[] buf, int minimumLength)
    {
        if (buf.Length >= minimumLength)
            return;

        buf = new T[FittingPowerOfTwo(minimumLength)];
    }

    /// <summary>
    /// Resize the given buffer to the next power of two that fits the needed size.
    /// Takes an array pool to rent/return with.
    /// The contents of the buffer are NOT preserved across resizes.
    /// </summary>
    public static void EnsurePooledBuffer<T>(ref T[] buf, ArrayPool<T> pool, int minimumLength)
    {
        if (buf.Length >= minimumLength)
            return;

        pool.Return(buf);
        buf = pool.Rent(minimumLength);
    }

    /// <summary>
    /// Calculate the smallest power of two that fits the required size.
    /// </summary>
    public static int FittingPowerOfTwo(int size) => 2 << BitOperations.Log2((uint)size - 1);
}
