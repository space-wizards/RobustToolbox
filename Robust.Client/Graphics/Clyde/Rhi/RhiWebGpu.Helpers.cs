using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Maths;
using Silk.NET.WebGPU;
using Color = Silk.NET.WebGPU.Color;
using RColor = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private static string WgpuVersionToString(uint version)
    {
        var a = (version >> 24) & 0xFF;
        var b = (version >> 16) & 0xFF;
        var c = (version >> 08) & 0xFF;
        var d = (version >> 00) & 0xFF;

        return $"{a}.{b}.{c}.{d}";
    }

    private static Color WgpuColor(RhiColor color) => new()
    {
        R = color.R,
        G = color.G,
        B = color.B,
        A = color.A
    };

    private static Extent3D WgpuExtent3D(RhiExtent3D extent)
    {
        return new Extent3D
        {
            Width = extent.Width,
            Height = extent.Height,
            DepthOrArrayLayers = extent.Depth
        };
    }

    private static Origin3D WgpuOrigin3D(RhiOrigin3D origin)
    {
        return new Origin3D
        {
            X = origin.X,
            Y = origin.Y,
            Z = origin.Z
        };
    }

    private static RhiTextureFormat ValidateTextureFormat(RhiTextureFormat format)
    {
        if (format is 0 or >= RhiTextureFormat.Final)
            throw new ArgumentException($"Invalid {nameof(RhiTextureFormat)}");

        return format;
    }

    private static RhiTextureDimension ValidateTextureDimension(RhiTextureDimension dimension)
    {
        if (dimension > RhiTextureDimension.Dim3D)
            throw new ArgumentException($"Invalid {nameof(RhiTextureDimension)}");

        return dimension;
    }

    private static RhiTextureUsage ValidateTextureUsage(RhiTextureUsage usage)
    {
        if (usage >= RhiTextureUsage.Final)
            throw new ArgumentException($"Invalid {nameof(RhiTextureUsage)}");

        return usage;
    }

    private static RhiTextureViewDimension ValidateTextureViewDimension(RhiTextureViewDimension dimension)
    {
        if (dimension >= RhiTextureViewDimension.Final)
            throw new ArgumentException($"Invalid {nameof(RhiTextureViewDimension)}");

        return dimension;
    }

    private static RhiTextureAspect ValidateTextureAspect(RhiTextureAspect aspect)
    {
        if (aspect >= RhiTextureAspect.Final)
            throw new ArgumentException($"Invalid {nameof(RhiTextureAspect)}");

        return aspect;
    }

    private static RhiAddressMode ValidateAddressMode(RhiAddressMode addressMode)
    {
        if (addressMode >= RhiAddressMode.Final)
            throw new ArgumentException($"Invalid {nameof(RhiAddressMode)}");

        return addressMode;
    }

    private static RhiFilterMode ValidateFilterMode(RhiFilterMode filterMode)
    {
        if (filterMode >= RhiFilterMode.Final)
            throw new ArgumentException($"Invalid {nameof(RhiFilterMode)}");

        return filterMode;
    }

    private static RhiMipmapFilterMode ValidateMipmapFilterMode(RhiMipmapFilterMode mipmapFilterMode)
    {
        if (mipmapFilterMode >= RhiMipmapFilterMode.Final)
            throw new ArgumentException($"Invalid {nameof(RhiMipmapFilterMode)}");

        return mipmapFilterMode;
    }

    private static RhiCompareFunction ValidateCompareFunction(RhiCompareFunction compareFunction)
    {
        if (compareFunction >= RhiCompareFunction.Final)
            throw new ArgumentException($"Invalid {nameof(RhiCompareFunction)}");

        return compareFunction;
    }

    private static PowerPreference ValidatePowerPreference(RhiPowerPreference powerPreference)
    {
        if (powerPreference >= RhiPowerPreference.Final)
            throw new ArgumentException($"Invalid {nameof(RhiPowerPreference)}");

        return (PowerPreference) powerPreference;
    }

    private static string MarshalFromString(byte* str)
    {
        return Marshal.PtrToStringUTF8((nint)str)!;
    }

    [return: NotNullIfNotNull(nameof(label))]
    private static byte[]? MakeLabel(string? label)
    {
        // TODO: Replace with stackalloc

        if (label == null)
            return null;

        return Encoding.UTF8.GetBytes(label);
    }

    /// <param name="buf">Must be pinned memory or I WILL COME TO YOUR HOUSE!!</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void* BumpAllocate(ref Span<byte> buf, int size)
    {
        // Round up to 8 to make sure everything stays aligned inside.
        var alignedSize = MathHelper.CeilingPowerOfTwo(size, 8);
        if (buf.Length < alignedSize)
            ThrowBumpAllocOutOfSpace();

        var ptr = Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(buf));
        buf = buf[alignedSize..];
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T* BumpAllocate<T>(ref Span<byte> buf) where T : unmanaged
    {
        var ptr = (T*)BumpAllocate(ref buf, sizeof(T));
        // Yeah I don't trust myself.
        *ptr = default;
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T* BumpAllocate<T>(ref Span<byte> buf, int count) where T : unmanaged
    {
        var size = checked(sizeof(T) * count);
        var ptr = BumpAllocate(ref buf, size);
        // Yeah I don't trust myself.
        new Span<byte>(ptr, size).Clear();
        return (T*)ptr;
    }

    // Workaround for C# not having pointers in generics.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T** BumpAllocatePtr<T>(ref Span<byte> buf, int count) where T : unmanaged
    {
        var size = checked(sizeof(T*) * count);
        var ptr = BumpAllocate(ref buf, size);
        // Yeah I don't trust myself.
        new Span<byte>(ptr, size).Clear();
        return (T**)ptr;
    }

    private static byte* BumpAllocateUtf8(ref Span<byte> buf, string? str)
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

    private sealed class WgpuPromise<TResult> : IDisposable
    {
        private readonly TaskCompletionSource<TResult> _tcs;
        private GCHandle _gcHandle;
        public Task<TResult> Task => _tcs.Task;
        public void* UserData => (void*) GCHandle.ToIntPtr(_gcHandle);

        public WgpuPromise()
        {
            _tcs = new TaskCompletionSource<TResult>();
            _gcHandle = GCHandle.Alloc(this);
        }

        public static void SetResult(void* userdata, TResult result)
        {
            var self = (WgpuPromise<TResult>)GCHandle.FromIntPtr((nint) userdata).Target!;
            self._tcs.SetResult(result);
        }

        public void Dispose()
        {
            _gcHandle.Free();
        }
    }
}
