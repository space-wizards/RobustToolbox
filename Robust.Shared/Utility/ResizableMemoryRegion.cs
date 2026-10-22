using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Maths;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.MEM;
using static TerraFX.Interop.Windows.PAGE;

namespace Robust.Shared.Utility;

/// <summary>
/// Implementation detail to store metrics for <see cref="ResizableMemoryRegion{T}"/>.
/// </summary>
internal static class ResizableMemoryRegionMetrics
{
    public static readonly Meter Meter = new("Robust.ResizableMemoryRegion");

    public const string GaugeName = "used_bytes";
}

// TODO: Proper implementation on Linux that uses mmap()/madvise()/mprotect().

/// <summary>
/// An unmanaged region of memory that can be dynamically resized without requiring copying.
/// </summary>
/// <remarks>
/// <para>
/// The maximum size of the memory region must be specified in the constructor. This reserves virtual memory for later,
/// but does not charge actual physical memory or commit charge and therefore costs nothing.
/// </para>
/// <para>
/// The "real" allocated memory region can be expanded by calling <see cref="Expand"/>.
/// This will increase resource consumption and make more memory available for use.
/// </para>
/// <para>
/// Allocated memory starts initialized to 0.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of elements stored in the memory region.</typeparam>
internal sealed unsafe class ResizableMemoryRegion<T> : IDisposable where T : unmanaged
{
    private static readonly KeyValuePair<string, object?>[] MetricTags =
        [new KeyValuePair<string, object?>("type", typeof(T).FullName)];

    // ReSharper disable once StaticMemberInGenericType
    private static long _memoryUsed;

    static ResizableMemoryRegion()
    {
        ResizableMemoryRegionMetrics.Meter.CreateObservableUpDownCounter(
            ResizableMemoryRegionMetrics.GaugeName,
            () => new Measurement<long>(_memoryUsed, MetricTags),
            "bytes",
            "The amount of committed memory used by ResizableMemoryRegion<T> instances.");
    }

    /// <summary>
    /// The pointer to the start of the allocated memory region. Use with care!
    /// </summary>
    public T* BaseAddress { get; private set; }

    /// <summary>
    /// The maximum amount of elements that can be stored in this memory region.
    /// </summary>
    public int MaxSize { get; }

    /// <summary>
    /// The current space (in elements) commit that is directly accessible.
    /// </summary>
    public int CurrentSize { get; private set; }

    /// <summary>
    /// Create a new <see cref="ResizableMemoryRegion{T}"/> with a certain maximum and initial size.
    /// </summary>
    /// <param name="maxElementSize">The maximum amount of elements ever stored in this memory region.</param>
    /// <param name="initialElementSize">
    /// The initial amount of elements that will be immediately accessible without using <see cref="Expand"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the memory region is already initialized.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="maxElementSize"/> is zero
    /// or <paramref name="initialElementSize"/> is greater than <paramref name="maxElementSize"/>.
    /// </exception>
    public ResizableMemoryRegion(int maxElementSize, int initialElementSize = 0)
    {
        if (BaseAddress != null)
            throw new InvalidOperationException("Memory region is already initialized!");

        if (initialElementSize > maxElementSize)
            throw new ArgumentException("initialSize must be smaller than maxSize");

        if (maxElementSize == 0)
            throw new ArgumentException("Cannot allocate a 0-byte memory region!");

        var maxByteSize = checked((nuint)sizeof(T) * (nuint)maxElementSize);

        if (OperatingSystem.IsWindows())
        {
            // On Windows, we MEM_RESERVE a large chunk of memory and then MEM_COMMIT it later when expanding.

            BaseAddress = (T*)VirtualAlloc(null, maxByteSize, MEM_RESERVE, PAGE_NOACCESS);
            if (BaseAddress == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        else
        {
            // Non-Windows systems use some form of overcommit,
            // and therefore we don't need to separately reserve and commit memory.
            // So we can just calloc() a fuck-huge chunk of memory and call it a day.
            //
            // It's important that we use calloc() and don't manually fill the memory region,
            // as that will avoid immediately assigning unused memory pages.
            //
            // Note that we still pretend to client code that this works the same as on Windows,
            // e.g. the memory is not writable until expanded into. We do this so that client code does not prematurely
            // populate the memory, e.g. PVS code filling it with a free list.
            BaseAddress = (T*)NativeMemory.AllocZeroed(maxByteSize);

            // What about Linux with overcommit disabled? Not a real use case, ignored.
        }

        MaxSize = maxElementSize;

        Expand(initialElementSize);
    }

    /// <summary>
    /// Expand the committed space for this <see cref="ResizableMemoryRegion{T}"/> to have space for at least
    /// <paramref name="newElementSize"/> elements.
    /// </summary>
    /// <remarks>
    /// This operation happens without copying and existing references to the memory region remain valid.
    /// </remarks>
    /// <param name="newElementSize">The minimum amount of elements that should fit in the memory region.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="newElementSize"/> is greater than <see cref="MaxSize"/>.
    /// </exception>
    public void Expand(int newElementSize)
    {
        ThrowIfDisposed();

        if (newElementSize > MaxSize)
            throw new ArgumentException("Cannot expand memory region past max size.", nameof(newElementSize));

        if (newElementSize <= CurrentSize)
            return;

        var previousSize = CurrentSize;

        var newByteSize = (nuint)sizeof(T) * (nuint)newElementSize;

        if (OperatingSystem.IsWindows())
        {
            var ret = VirtualAlloc(BaseAddress, newByteSize, MEM_COMMIT, PAGE_READWRITE);
            if (ret == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        else
        {
            // Nada. On overcommit systems we don't need to do anything.
        }

        CurrentSize = newElementSize;

        Interlocked.Add(ref _memoryUsed, (newElementSize - previousSize) * sizeof(T));
    }

    /// <summary>
    /// Shrink this <see cref="ResizableMemoryRegion{T}"/> to reduce the amount of memory used.
    /// </summary>
    /// <remarks>
    /// Existing references inside the shrank-away region of memory become undefined to read or write from,
    /// and can cause access violations.
    /// </remarks>
    /// <param name="newElementSize">
    /// The new size of the committed region of memory.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if trying to shrink to a size larger than the current size, or when trying to shrink to a negative size.
    /// </exception>
    public void Shrink(int newElementSize)
    {
        ThrowIfDisposed();

        if (newElementSize > CurrentSize)
            throw new ArgumentException("Cannot shrink to a larger size!", nameof(newElementSize));

        if (newElementSize < 0)
            throw new ArgumentException("Cannot shrink to a negative size!", nameof(newElementSize));

        var currentByteSize = (nuint)sizeof(T) * (nuint)CurrentSize;
        var newByteSize = (nuint)sizeof(T) * (nuint)newElementSize;

        // If the new max size cuts a page in the middle we can't free it so round up to the next page.
        var newPageSize = MathHelper.CeilMultipleOfPowerOfTwo(newByteSize, (nuint)Environment.SystemPageSize);
        if (OperatingSystem.IsWindows())
        {
            var freeBaseAddress = (byte*)BaseAddress + newPageSize;
            var freeLength = currentByteSize - newPageSize;
            var result = VirtualFree(freeBaseAddress, freeLength, MEM_DECOMMIT);
            if (!result)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        else
        {
            // Nothing to do on operating systems without advanced memory management.
        }

        CurrentSize = newElementSize;

        Interlocked.Add(ref _memoryUsed, (long)(newByteSize - currentByteSize));
    }

    /// <summary>
    /// Get a <see cref="Span{T}"/> over the committed region of memory.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> over the committed region of memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan()
    {
        // If the memory region is disposed, CurrentSize is 0 so it's impossible to use the (nullptr) BaseAddress.
        // This means you can't dereference the span anyways, so that works out fine!
        return new Span<T>(BaseAddress, CurrentSize);
    }

    /// <summary>
    /// Get a <see cref="Span{T}"/> over the committed region of memory, cast to a different type.
    /// </summary>
    /// <remarks>
    /// This is equivalent to using <see cref="MemoryMarshal.Cast{TFrom, TTo}(Span{TFrom})"/> on the result of <see cref="GetSpan"/>.
    /// </remarks>
    /// <typeparam name="TCast">The type to cast the memory region to.</typeparam>
    /// <returns>A <see cref="Span{T}"/> over the committed region of memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<TCast> GetSpan<TCast>() where TCast : unmanaged
    {
        return MemoryMarshal.Cast<T, TCast>(GetSpan());
    }

    /// <summary>
    /// Get a mutable reference to a single element of the memory region.
    /// </summary>
    /// <param name="index">The index of the element desired.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if <paramref name="index"/> is greater or equal to <see cref="CurrentSize"/>.
    /// </exception>
    /// <returns>A mutable reference to the element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
        // If the memory region is disposed, CurrentSize is 0 and this check always fails.
        if (index >= CurrentSize || index < 0)
            ThrowIndexOutOfRangeException(CurrentSize, index);

        return ref *(BaseAddress + index);
    }

    /// <summary>
    /// Get a mutable reference to a single element of the memory region, cast to a different type.
    /// </summary>
    /// <remarks>
    /// This is equivalent to using <see cref="M:System.Runtime.CompilerServices.Unsafe.As``2(``0@)"/> on the result of <see cref="GetSpan"/>.
    /// </remarks>
    /// <param name="index">The index of the element desired.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if <paramref name="index"/> is greater or equal to <see cref="CurrentSize"/>.
    /// </exception>
    /// <returns>A mutable reference to the element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TCast GetRef<TCast>(int index) where TCast : unmanaged
    {
        return ref Unsafe.As<T, TCast>(ref GetRef(index));
    }

    /// <summary>
    /// Clear the contents of the memory stored in this <see cref="ResizableMemoryRegion{T}"/> back to zero.
    /// </summary>
    /// <remarks>
    /// This does not change the <see cref="CurrentSize"/>.
    /// </remarks>
    public void Clear()
    {
        GetSpan().Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (BaseAddress == null)
            ThrowNotInitialized();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNotInitialized()
    {
        throw new InvalidOperationException("Memory region is not initialized!");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRangeException(int size, int index)
    {
        throw new IndexOutOfRangeException($"Index was outside the bounds of the memory region. Size: {size}, Index: {index}");
    }

    private void ReleaseUnmanagedResources()
    {
        if (BaseAddress == null)
            return;

        if (OperatingSystem.IsWindows())
        {
            var result = VirtualFree(BaseAddress, 0, MEM_RELEASE);
            if (!result)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        else
        {
            NativeMemory.Free(BaseAddress);
        }

        Interlocked.Add(ref _memoryUsed, -CurrentSize * sizeof(T));

        BaseAddress = null;
        CurrentSize = 0;
    }

    /// <summary>
    /// Release the backing memory for this <see cref="ResizableMemoryRegion{T}"/>.
    /// </summary>
    /// <remarks>
    /// Existing references to memory of the memory region become invalid and should no longer be used.
    /// </remarks>
    public void Dispose()
    {
        ReleaseUnmanagedResources();

        GC.SuppressFinalize(this);
    }

    ~ResizableMemoryRegion()
    {
        ReleaseUnmanagedResources();
    }
}
