using System;
using JetBrains.Annotations;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // Go through the commit log if you wanna find why this struct exists.
        // And why there's no implicit operator.
        /// <summary>
        ///     Basically just a handle around the integer object handles returned by OpenGL.
        /// </summary>
        [PublicAPI]
        private struct GLHandle : IEquatable<GLHandle>
        {
            public readonly uint Handle;

            public GLHandle(int handle) : this((uint) handle)
            {
            }

            public GLHandle(uint handle)
            {
                Handle = handle;
            }

            public bool Equals(GLHandle other)
            {
                return Handle == other.Handle;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is GLHandle other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Handle.GetHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(GLHandle)}: {Handle}";
            }

            public static bool operator ==(GLHandle a, GLHandle b)
            {
                return a.Handle == b.Handle;
            }

            public static bool operator !=(GLHandle a, GLHandle b)
            {
                return a.Handle != b.Handle;
            }
        }

    }
}
