using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

internal static unsafe class FfiHelper
{
    internal static Span<byte> CreateHeapBumpAllocateBuffer(int size, out void* ptr)
    {
        ptr = NativeMemory.Alloc(checked((nuint)size));

        return new Span<byte>(ptr, size);
    }

    internal static bool TryBumpAllocate(ref Span<byte> buf, int size, out void* ptr)
    {
        // Round up to 8 to make sure everything stays aligned inside.
        var alignedSize = MathHelper.CeilingPowerOfTwo(size, 8);
        if (buf.Length < alignedSize)
        {
            ptr = null;
            return false;
        }

        ptr = Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(buf));
        buf = buf[alignedSize..];
        return true;
    }

    /// <param name="buf">Must be pinned memory or I WILL COME TO YOUR HOUSE!!</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* BumpAllocate(ref Span<byte> buf, int size)
    {
        if (!TryBumpAllocate(ref buf, size, out var ptr))
            ThrowBumpAllocOutOfSpace();

        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryBumpAllocate<T>(ref Span<byte> buf, out T* ptr) where T : unmanaged
    {
        if (TryBumpAllocate(ref buf, sizeof(T), out var voidPtr))
        {
            ptr = (T*)voidPtr;
            // Yeah I don't trust myself.
            *ptr = default;
            return true;
        }

        ptr = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T* BumpAllocate<T>(ref Span<byte> buf) where T : unmanaged
    {
        var ptr = (T*)BumpAllocate(ref buf, sizeof(T));
        // Yeah I don't trust myself.
        *ptr = default;
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryBumpAllocate<T>(ref Span<byte> buf, int count, out T* ptr) where T : unmanaged
    {
        var size = checked(sizeof(T) * count);
        if (TryBumpAllocate(ref buf, size, out var voidPtr))
        {
            ptr = (T*)voidPtr;
            new Span<byte>(ptr, size).Clear();
            return true;
        }

        ptr = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryBumpAllocate<T>(
        ref Span<byte> buf,
        int count,
        out T* ptr,
        out Span<T> span)
        where T : unmanaged
    {
        var size = checked(sizeof(T) * count);
        if (TryBumpAllocate(ref buf, size, out var voidPtr))
        {
            ptr = (T*)voidPtr;
            span = new Span<T>(ptr, size);
            // Yeah I don't trust myself.
            span.Clear();
            return true;
        }

        ptr = null;
        span = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T* BumpAllocate<T>(ref Span<byte> buf, int count) where T : unmanaged
    {
        var size = checked(sizeof(T) * count);
        var ptr = BumpAllocate(ref buf, size);
        // Yeah I don't trust myself.
        new Span<byte>(ptr, size).Clear();
        return (T*)ptr;
    }

    // Workaround for C# not having pointers in generics.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T** BumpAllocatePtr<T>(ref Span<byte> buf, int count) where T : unmanaged
    {
        var size = checked(sizeof(T*) * count);
        var ptr = BumpAllocate(ref buf, size);
        // Yeah I don't trust myself.
        new Span<byte>(ptr, size).Clear();
        return (T**)ptr;
    }

    internal static bool TryBumpAllocateUtf8(ref Span<byte> buf, string? str, out byte* ptr)
    {
        if (str == null)
        {
            ptr = null;
            return true;
        }

        ptr = (byte*) Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(buf));

        if (!Encoding.UTF8.TryGetBytes(str, buf, out var written) || written == buf.Length)
        {
            ptr = null;
            return false;
        }

        buf[written] = 0; // Nul terminator
        buf = buf[(written + 1)..];
        return true;
    }

    internal static byte* BumpAllocateUtf8(ref Span<byte> buf, string? str)
    {
        if (str == null)
            return null;

        var byteCount = Encoding.UTF8.GetByteCount(str) + 1;
        var ptr = BumpAllocate(ref buf, byteCount);
        var dstSpan = new Span<byte>(ptr, byteCount);
        Encoding.UTF8.GetBytes(str, dstSpan);
        dstSpan[^1] = 0;

        return (byte*) ptr;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBumpAllocOutOfSpace()
    {
        throw new InvalidOperationException("Out of bump allocator space!");
    }
}
