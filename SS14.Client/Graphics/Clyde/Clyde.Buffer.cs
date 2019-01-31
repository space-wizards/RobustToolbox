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
            private readonly Clyde _clyde;
            public BufferTarget Type { get; }
            public int Size { get; private set; }
            public int Handle { get; private set; }
            public BufferUsageHint UsageHint { get; }
            public string Name { get; }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, string name=null)
            {
                _clyde = clyde;
                Type = type;
                Name = name;
                UsageHint = usage;

                Handle = GL.GenBuffer();
            }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, int size, string name=null)
                : this(clyde, type, usage, name)
            {
                Reallocate(size);
            }

            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, Span<byte> initialize, string name=null)
                : this(clyde, type, usage, name)
            {
                Reallocate(initialize);
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

                var byteSpan = MemoryMarshal.AsBytes(data);

                unsafe
                {
                    fixed (byte* ptr = byteSpan)
                    {
                        GL.BufferSubData(Type, (IntPtr)start, byteSpan.Length, (IntPtr)ptr);
                    }
                }
            }

            public void Reallocate<T>(Span<T> data) where T : unmanaged
            {
                Use();

                var byteSpan = MemoryMarshal.AsBytes(data);

                unsafe
                {
                    fixed (byte* ptr = byteSpan)
                    {
                        GL.BufferData(Type, byteSpan.Length, (IntPtr)ptr, UsageHint);
                    }
                }
            }

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
            public Buffer(Clyde clyde, BufferTarget type, BufferUsageHint usage, Span<T> initialize, string name=null)
                : base(clyde, type, usage, MemoryMarshal.AsBytes(initialize), name)
            {
            }
        }
    }
}
