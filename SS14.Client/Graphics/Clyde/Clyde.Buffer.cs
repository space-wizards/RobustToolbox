using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     Represents an OpenGL buffer object.
        /// </summary>
        private class Buffer
        {
            public BufferTarget Type { get; }
            public int Size { get; }
            public int Handle { get; private set; }

            public Buffer(BufferTarget type, BufferUsageHint usage, int size)
            {
                Type = type;
                Size = size;

                Handle = GL.GenBuffer();
                GL.BindBuffer(type, Handle);
                GL.BufferData(type, size, IntPtr.Zero, usage);
            }

            public Buffer(BufferTarget type, BufferUsageHint usage, Span<byte> initialize)
            {
                Type = type;
                Size = initialize.Length;

                Handle = GL.GenBuffer();
                GL.BindBuffer(type, Handle);

                unsafe
                {
                    fixed (byte* ptr = &MemoryMarshal.GetReference(MemoryMarshal.AsBytes(initialize)))
                    {
                        GL.BufferData(type, Size, (IntPtr)ptr, usage);
                    }
                }
            }

            public void Use()
            {
                DebugTools.Assert(Handle != -1);

                GL.BindBuffer(Type, Handle);
            }

            public void Delete()
            {
                GL.DeleteBuffer(Handle);
                Handle = -1;
            }

            public void WriteSubData<T>(int start, Span<T> data) where T : unmanaged
            {
                Use();

                unsafe
                {
                    fixed (T* ptr = data)
                    {
                        GL.BufferSubData(Type, (IntPtr)start, data.Length, (IntPtr)ptr);
                    }
                }
            }

            public void Reallocate<T>(Span<T> data, BufferUsageHint usageHint) where T : unmanaged
            {
                Use();

                unsafe
                {
                    fixed (T* ptr = data)
                    {
                        GL.BufferData(Type, data.Length, (IntPtr)ptr, usageHint);
                    }
                }
            }

            public void Reallocate(int size, BufferUsageHint usageHint)
            {
                Use();

                GL.BufferData(Type, size, IntPtr.Zero, usageHint);
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Subtype of buffers so that we can have a generic constructor.
        ///     Functionally equivalent to <see cref="Buffer"/> otherwise.
        /// </summary>
        private class Buffer<T> : Buffer where T : unmanaged
        {
            public Buffer(BufferTarget type, BufferUsageHint usage, Span<T> initialize)
                : base(type, usage, MemoryMarshal.AsBytes(initialize))
            {
            }
        }
    }
}
