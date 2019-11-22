using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Shaders
{
    /// <summary>
    ///     A shader instance is a wrapper around a shader that can be used for rendering.
    ///     It can contain extra, mutable parameters depending on the shader backing it.
    /// </summary>
    /// <remarks>
    ///     Shader instances are often shared to reduce bloat.
    ///     As such, they can be made "immutable" to avoid accidentally modifying
    ///     the shader instance used by every entity on the station.
    ///
    ///     A shader can be made immutable with <see cref="MakeImmutable" />. This is irreversible.
    ///     After doing this, operations such as <see cref="SetParameter(string, float)"/> will throw a <see cref="InvalidOperationException"/>.
    ///
    ///     You can "duplicate" a shader instance to make a separate,
    ///     once again mutable, copy with <see cref="Duplicate"/>.
    /// </remarks>
    public abstract class ShaderInstance : IDisposable
    {
        public bool Disposed { get; private set; }

        /// <summary>
        ///     Whether this shader is mutable. An immutable shader can no longer be edited and is ideal for sharing.
        /// </summary>
        public bool Mutable { get; private set; } = true;

        public void SetParameter(string name, float value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Vector2 value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Vector3 value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Vector4 value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Color value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, int value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Vector2i value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, bool value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, in Matrix3 value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, in Matrix4 value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, Texture value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        /// <summary>
        ///     Make this shader permanently immutable.
        /// </summary>
        public void MakeImmutable()
        {
            Mutable = false;
        }

        /// <summary>
        ///     Make an identical copy of this shader, that is treated separately for the parameters assigned to it.
        ///     The copy is also mutable, even if the source is not so.
        /// </summary>
        public ShaderInstance Duplicate()
        {
            EnsureAlive();

            return DuplicateImpl();
        }

        ~ShaderInstance()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        private void EnsureMutable()
        {
            if (!Mutable)
            {
                throw new InvalidOperationException(
                    "This shader instance is immutable and cannot be modified. Duplicate it instead.");
            }
        }

        private void EnsureAlive()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(ShaderInstance));
            }
        }

        protected abstract ShaderInstance DuplicateImpl();

        protected abstract void SetParameterImpl(string name, float value);
        protected abstract void SetParameterImpl(string name, Vector2 value);
        protected abstract void SetParameterImpl(string name, Vector3 value);
        protected abstract void SetParameterImpl(string name, Vector4 value);
        protected abstract void SetParameterImpl(string name, Color value);
        protected abstract void SetParameterImpl(string name, int value);
        protected abstract void SetParameterImpl(string name, Vector2i value);
        protected abstract void SetParameterImpl(string name, bool value);
        protected abstract void SetParameterImpl(string name, in Matrix3 value);
        protected abstract void SetParameterImpl(string name, in Matrix4 value);
        protected abstract void SetParameterImpl(string name, Texture value);
    }
}
