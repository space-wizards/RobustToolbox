using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
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
        private bool _stencilTestEnabled;
        private int _stencilRef;
        private int _stencilReadMask;
        private int _stencilWriteMask;
        private StencilFunc _stencilFunc = StencilFunc.Always;
        private StencilOp _stencilOp = StencilOp.Keep;

        public bool Disposed { get; private set; }

        /// <summary>
        ///     Whether this shader is mutable. An immutable shader can no longer be edited and is ideal for sharing.
        /// </summary>
        public bool Mutable { get; private set; } = true;

        public bool StencilTestEnabled
        {
            get => _stencilTestEnabled;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilTestEnabled = value;
                SetStencilTestEnabledImpl(value);
            }
        }

        public int StencilRef
        {
            get => _stencilRef;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilRef = value;
                SetStencilRefImpl(value);
            }
        }

        public int StencilWriteMask
        {
            get => _stencilWriteMask;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilWriteMask = value;
                SetStencilWriteMaskImpl(value);
            }
        }

        public int StencilReadMask
        {
            get => _stencilReadMask;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilReadMask = value;
                SetStencilReadMaskRefImpl(value);
            }
        }

        public StencilFunc StencilFunc
        {
            get => _stencilFunc;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilFunc = value;
                SetStencilFuncImpl(value);
            }
        }

        public StencilOp StencilOp
        {
            get => _stencilOp;
            set
            {
                EnsureAlive();
                EnsureMutable();

                _stencilOp = value;
                SetStencilOpImpl(value);
            }
        }

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

        private protected abstract ShaderInstance DuplicateImpl();

        private protected abstract void SetParameterImpl(string name, float value);
        private protected abstract void SetParameterImpl(string name, Vector2 value);
        private protected abstract void SetParameterImpl(string name, Vector3 value);
        private protected abstract void SetParameterImpl(string name, Vector4 value);
        private protected abstract void SetParameterImpl(string name, Color value);
        private protected abstract void SetParameterImpl(string name, int value);
        private protected abstract void SetParameterImpl(string name, Vector2i value);
        private protected abstract void SetParameterImpl(string name, bool value);
        private protected abstract void SetParameterImpl(string name, in Matrix3 value);
        private protected abstract void SetParameterImpl(string name, in Matrix4 value);
        private protected abstract void SetParameterImpl(string name, Texture value);

        private protected abstract void SetStencilOpImpl(StencilOp op);
        private protected abstract void SetStencilFuncImpl(StencilFunc func);
        private protected abstract void SetStencilTestEnabledImpl(bool enabled);
        private protected abstract void SetStencilRefImpl(int @ref);
        private protected abstract void SetStencilWriteMaskImpl(int mask);
        private protected abstract void SetStencilReadMaskRefImpl(int mask);
    }
}
