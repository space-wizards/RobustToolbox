using System;
using System.Numerics;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

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
        public bool Disposed { get; protected set; }

        /// <summary>
        ///     Whether this shader is mutable. An immutable shader can no longer be edited and is ideal for sharing.
        /// </summary>
        [ViewVariables]
        public bool Mutable { get; private set; } = true;

        private StencilParameters _stencil;

        public StencilParameters Stencil
        {
            get => _stencil;
            set
            {
                EnsureAlive();
                EnsureMutable();
                _stencil = value;
                SetStencilImpl(_stencil);
            }
        }

        public bool StencilTestEnabled
        {
            get => _stencil.Enabled;
            set => Stencil = _stencil with { Enabled = value};
        }

        public int StencilWriteMask
        {
            get => _stencil.WriteMask;
            set => Stencil = _stencil with { WriteMask = value};
        }

        public int StencilReadMask
        {
            get => _stencil.ReadMask;
            set => Stencil = _stencil with { ReadMask = value};
        }

        public StencilFunc StencilFunc
        {
            get => _stencil.Func;
            set => Stencil = _stencil with { Func = value};
        }

        public StencilOp StencilOp
        {
            get => _stencil.Op;
            set => Stencil = _stencil with { Op = value};
        }

        public void SetParameter(string name, float value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, float[] value)
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

        public void SetParameter(string name, Vector2[] value)
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

        public void SetParameter(string name, Color[] value)
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

        public void SetParameter(string name, bool[] value)
        {
            EnsureAlive();
            EnsureMutable();
            SetParameterImpl(name, value);
        }

        public void SetParameter(string name, in Matrix3x2 value)
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

        public abstract void Dispose();

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
        private protected abstract void SetParameterImpl(string name, float[] value);
        private protected abstract void SetParameterImpl(string name, Vector2 value);
        private protected abstract void SetParameterImpl(string name, Vector2[] value);
        private protected abstract void SetParameterImpl(string name, Vector3 value);
        private protected abstract void SetParameterImpl(string name, Vector4 value);
        private protected abstract void SetParameterImpl(string name, Color value);
        private protected abstract void SetParameterImpl(string name, Color[] value);
        private protected abstract void SetParameterImpl(string name, int value);
        private protected abstract void SetParameterImpl(string name, Vector2i value);
        private protected abstract void SetParameterImpl(string name, bool value);
        private protected abstract void SetParameterImpl(string name, bool[] value);
        private protected abstract void SetParameterImpl(string name, in Matrix3x2 value);
        private protected abstract void SetParameterImpl(string name, in Matrix4 value);
        private protected abstract void SetParameterImpl(string name, Texture value);
        private protected abstract void SetStencilImpl(StencilParameters value);
    }

    [DataDefinition]
    public partial struct StencilParameters
    {
        public StencilParameters()
        {
        }

        [ViewVariables] public bool Enabled = false;
        [DataField("ref")] public int Ref = 0;
        [DataField("writeMask")] public int WriteMask = unchecked((int)uint.MaxValue);
        [DataField("readMask")] public int ReadMask = unchecked((int)uint.MaxValue);
        [DataField("op")] public StencilOp Op = StencilOp.Keep;
        [DataField("func")] public StencilFunc Func  = StencilFunc.Always;
    }
}
