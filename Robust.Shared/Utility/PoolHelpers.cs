using System;
using System.Buffers;

namespace Robust.Shared.Utility;

/// <summary>
/// Helpers for working with memory pooling such as <see cref="ArrayPool{T}"/>
/// </summary>
public static class PoolHelpers
{
    /// <summary>
    /// Provides a disposable guard to return an array pool entry.
    /// </summary>
    /// <remarks>
    /// This is intended to be used with using statements.
    /// </remarks>
    public static PoolReturnGuard<T> ReturnGuard<T>(this ArrayPool<T> pool, T[] buf)
    {
        return new PoolReturnGuard<T>(pool, buf);
    }

    /// <summary>
    /// Disposes the given array into the given array pool on dispose.
    /// </summary>
    public readonly struct PoolReturnGuard<T> : IDisposable
    {
        private readonly ArrayPool<T> _pool;
        private readonly T[] _array;

        public PoolReturnGuard(ArrayPool<T> pool, T[] array)
        {
            _pool = pool;
            _array = array;
        }

        public void Dispose()
        {
            _pool.Return(_array);
        }
    }
}
