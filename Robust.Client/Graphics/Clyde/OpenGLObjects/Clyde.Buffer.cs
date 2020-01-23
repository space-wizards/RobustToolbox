using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     Represents an OpenGL buffer object.
        /// </summary>
        private class Buffer
        {
            private readonly Clyde _clyde;
            public BufferTarget Type { get; }
            public uint ObjectHandle { get; private set; }
            public BufferUsageHint UsageHint { get; }
            public string Name { get; }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, string name = null)
            {
                _clyde = clyde;
                Type = type;
                Name = name;
                UsageHint = usage;

                GL.GenBuffers(1, out uint handle);
                ObjectHandle = handle;

                if (name != null)
                {
                    _clyde._objectLabelMaybe(ObjectLabelIdentifier.Buffer, ObjectHandle, name);
                }
            }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, int size, string name = null)
                : this(clyde, type, usage, name)
            {
                Reallocate(size);
            }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, Span<byte> initialize,
                string name = null)
                : this(clyde, type, usage, name)
            {
                Reallocate(initialize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Use()
            {
                DebugTools.Assert(ObjectHandle != 0);

                GL.BindBuffer(Type, ObjectHandle);
            }

            public void Delete()
            {
                GL.DeleteBuffer(ObjectHandle);
                ObjectHandle = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteSubData<T>(int start, Span<T> data) where T : unmanaged
            {
                Use();
                var byteSpan = MemoryMarshal.AsBytes(data);

                unsafe
                {
                    fixed (byte* ptr = byteSpan)
                    {
                        GL.BufferSubData(Type, (IntPtr) start, byteSpan.Length, (IntPtr) ptr);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteSubData<T>(Span<T> data) where T : unmanaged
            {
                Use();
                var byteSpan = MemoryMarshal.AsBytes(data);

                unsafe
                {
                    fixed (byte* ptr = byteSpan)
                    {
                        GL.BufferSubData(Type, IntPtr.Zero, byteSpan.Length, (IntPtr) ptr);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteSubData<T>(in T data) where T : unmanaged
            {
                Use();
                unsafe
                {
                    fixed (T* ptr = &data)
                    {
                        GL.BufferSubData(Type, IntPtr.Zero, sizeof(T), (IntPtr)ptr);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reallocate<T>(Span<T> data) where T : unmanaged
            {
                Use();
                var byteSpan = MemoryMarshal.AsBytes(data);

                unsafe
                {
                    fixed (byte* ptr = byteSpan)
                    {
                        GL.BufferData(Type, byteSpan.Length, (IntPtr) ptr, UsageHint);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reallocate<T>(in T data) where T : unmanaged
            {
                Use();
                unsafe
                {
                    fixed (T* ptr = &data)
                    {
                        GL.BufferData(Type, sizeof(T), (IntPtr)ptr, UsageHint);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reallocate(int size)
            {
                Use();
                GL.BufferData(Type, size, IntPtr.Zero, UsageHint);
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Subtype of buffers so that we can have a generic constructor.
        ///     Functionally equivalent to <see cref="Buffer"/> otherwise.
        /// </summary>
        private class Buffer<T> : Buffer where T : unmanaged
        {
            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, Span<T> initialize, string name = null)
                : base(clyde, type, usage, MemoryMarshal.AsBytes(initialize), name)
            {
            }
        }
    }
}
