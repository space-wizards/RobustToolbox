using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     This is an utility class. It does not check whether the OpenGL state machine is set up correctly.
        ///     You've been warned:
        ///     using things like <see cref="SetUniformTexture" /> if this buffer isn't bound WILL mess things up!
        /// </summary>
        private sealed class GLShaderProgram
        {
            private readonly sbyte?[] _uniformIntCache = new sbyte?[UniCount];
            private readonly Dictionary<string, int> _uniformCache = new();
            public uint Handle = 0;
            private GLShader? _fragmentShader;
            private GLShader? _vertexShader;
            public string? Name { get; }
            private readonly Clyde _clyde;

            public GLShaderProgram(Clyde clyde, string? name = null)
            {
                _clyde = clyde;
                Name = name;
            }

            public void Add(GLShader shader)
            {
                ClearCaches();
                switch (shader.Type)
                {
                    case ShaderType.VertexShader:
                        _vertexShader = shader;
                        break;
                    case ShaderType.FragmentShader:
                        _fragmentShader = shader;
                        break;
                    default:
                        throw new NotImplementedException("Tried to add unsupported shader type!");
                }
            }

            public void Link((string, uint)[] attribLocations)
            {
                ClearCaches();
                Handle = (uint) GL.CreateProgram();
                _clyde.CheckGlError();
                if (Name != null)
                {
                    _clyde.ObjectLabelMaybe(ObjectLabelIdentifier.Program, Handle, Name);
                }

                if (_vertexShader != null)
                {
                    GL.AttachShader(Handle, _vertexShader.ObjectHandle);
                    _clyde.CheckGlError();
                }

                if (_fragmentShader != null)
                {
                    GL.AttachShader(Handle, _fragmentShader.ObjectHandle);
                    _clyde.CheckGlError();
                }

                foreach (var (varName, loc) in attribLocations)
                {
                    // OpenGL 3.1 is ass and doesn't allow you to specify layout(location = X) in shaders.
                    // So we have to manually do it here.
                    // Ugh.

                    GL.BindAttribLocation(Handle, loc, varName);
                    _clyde.CheckGlError();
                }

                GL.LinkProgram(Handle);
                _clyde.CheckGlError();

                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var compiled);
                _clyde.CheckGlError();
                if (compiled != 1)
                {
                    throw new ShaderCompilationException(GL.GetProgramInfoLog((int) Handle));
                }
            }

            public void Use()
            {
                if (_clyde._currentProgram == this)
                {
                    return;
                }

                ForceUse();
            }

            public void ForceUse()
            {
                DebugTools.Assert(Handle != 0);

                _clyde._currentProgram = this;
                GL.UseProgram(Handle);
                _clyde.CheckGlError();
            }

            public void Delete()
            {
                if (Handle == 0)
                {
                    return;
                }

                GL.DeleteProgram(Handle);
                _clyde.CheckGlError();
                Handle = 0;
            }

            public int GetUniform(string name)
            {
                if (!TryGetUniform(name, out var result))
                {
                    ThrowCouldNotGetUniform(name);
                }

                return result;
            }

            public int GetUniform(int id)
            {
                if (!TryGetUniform(id, out var result))
                {
                    ThrowCouldNotGetUniform($"[id {id}]");
                }

                return result;
            }

            public bool TryGetUniform(string name, out int index)
            {
                DebugTools.Assert(Handle != 0);

                if (_uniformCache.TryGetValue(name, out index))
                {
                    return index != -1;
                }

                index = GL.GetUniformLocation(Handle, name);
                _clyde.CheckGlError();
                _uniformCache.Add(name, index);
                return index != -1;
            }

            public bool TryGetUniform(int id, out int index)
            {
                DebugTools.Assert(Handle != 0);
                DebugTools.Assert(id < UniCount);

                var value = _uniformIntCache[id];
                if (value.HasValue)
                {
                    index = value.Value;
                    return index != -1;
                }

                return InitIntUniform(id, out index);
            }

            private bool InitIntUniform(int id, out int index)
            {
                string name;
                switch (id)
                {
                    case UniIModUV:
                        name = UniModUV;
                        break;
                    case UniILightTexture:
                        name = UniLightTexture;
                        break;
                    case UniIMainTexture:
                        name = UniMainTexture;
                        break;
                    case UniIModelMatrix:
                        name = UniModelMatrix;
                        break;
                    case UniITexturePixelSize:
                        name = UniTexturePixelSize;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                index = GL.GetUniformLocation(Handle, name);
                _clyde.CheckGlError();
                _uniformIntCache[id] = (sbyte)index;
                return index != -1;
            }

            public bool HasUniform(string name) => TryGetUniform(name, out _);
            public bool HasUniform(int id) => TryGetUniform(id, out _);

            public void BindBlock(string blockName, uint blockBinding)
            {
                var index = (uint) GL.GetUniformBlockIndex(Handle, blockName);
                _clyde.CheckGlError();
                GL.UniformBlockBinding(Handle, index, blockBinding);
                _clyde.CheckGlError();
            }

            public void SetUniform(string uniformName, int integer)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, integer);
                _clyde.CheckGlError();
            }

            public void SetUniform(string uniformName, float single)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, single);
                _clyde.CheckGlError();
            }

            public void SetUniform(int uniformName, float single)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, single);
            }

            public void SetUniform(string uniformName, float[] singles)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, singles.Length, singles);
                _clyde.CheckGlError();
            }

            public void SetUniform(int uniformName, float[] singles)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, singles.Length, singles);
            }

            public void SetUniform(string uniformName, in Matrix3x2 matrix)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix);
            }

            public void SetUniform(int uniformName, in Matrix3x2 matrix)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void SetUniformDirect(int slot, in Matrix3x2 value)
            {
                // We put the rows of the input matrix into the columns of our GPU matrices
                // this transpose is required, as in C#, we premultiply vectors with matrices
                // (vM) while GL postmultiplies vectors with matrices (Mv); however, since
                // the Matrix3x2 data is stored row-major, and GL uses column-major, the
                // memory layout is the same (or would be, if Matrix3x2 didn't have an
                // implicit column)
                var buf = stackalloc float[9]{
                    value.M11, value.M12, 0,
                    value.M21, value.M22, 0,
                    value.M31, value.M32, 1
                };
                GL.UniformMatrix3(slot, 1, false, (float*)buf);
                _clyde.CheckGlError();
            }

            public void SetUniform(string uniformName, in Matrix4 matrix, bool transpose=true)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix, transpose);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void SetUniformDirect(int uniformId, in Matrix4 value, bool transpose=true)
            {
                Matrix4 tmpTranspose = value;
                if (transpose)
                {
                    // transposition not supported on GLES2, & no access to _hasGLES
                    tmpTranspose.Transpose();
                }
                GL.UniformMatrix4(uniformId, 1, false, (float*) &tmpTranspose);
                _clyde.CheckGlError();
            }

            public void SetUniform(string uniformName, in Vector4 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            public void SetUniform(int uniformName, in Vector4 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformDirect(int slot, in Vector4 vector)
            {
                unsafe
                {
                    fixed (Vector4* ptr = &vector)
                    {
                        GL.Uniform4(slot, 1, (float*)ptr);
                        _clyde.CheckGlError();
                    }
                }
            }

            public void SetUniform(string uniformName, in Color color, bool convertToLinear = true)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, color, convertToLinear);
            }

            public void SetUniform(int uniformName, in Color color, bool convertToLinear = true)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, color, convertToLinear);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformDirect(int slot, in Color color, bool convertToLinear=true)
            {
                var converted = color;
                if (convertToLinear)
                {
                    converted = Color.FromSrgb(color);
                }

                unsafe
                {
                    GL.Uniform4(slot, 1, (float*) &converted);
                    _clyde.CheckGlError();
                }
            }

            public void SetUniform(string uniformName, in Vector3 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformDirect(int slot, in Vector3 vector)
            {
                unsafe
                {
                    fixed (Vector3* ptr = &vector)
                    {
                        GL.Uniform3(slot, 1, (float*)ptr);
                        _clyde.CheckGlError();
                    }
                }
            }

            public void SetUniform(string uniformName, in Vector2 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            public void SetUniform(int uniformName, in Vector2 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformDirect(int slot, in Vector2 vector)
            {
                unsafe
                {
                    fixed (Vector2* ptr = &vector)
                    {
                        GL.Uniform2(slot, 1, (float*)ptr);
                        _clyde.CheckGlError();
                    }
                }
            }

            public void SetUniform(string uniformName, Vector2[] vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            public void SetUniform(int uniformName, Vector2[] vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformDirect(int slot, Vector2[] vectors)
            {
                unsafe
                {
                    fixed (Vector2* ptr = &vectors[0])
                    {
                        GL.Uniform2(slot, vectors.Length, (float*)ptr);
                        _clyde.CheckGlError();
                    }
                }
            }

            public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformTextureDirect(uniformId, textureUnit);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetUniformTextureDirect(int slot, TextureUnit value)
            {
                GL.Uniform1(slot, value - TextureUnit.Texture0);
                _clyde.CheckGlError();
            }

            public void SetUniformTextureMaybe(string uniformName, TextureUnit value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformTextureDirect(slot, value);
                }
            }

            public void SetUniformTextureMaybe(int uniformName, TextureUnit value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformTextureDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Vector4 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(int uniformName, in Vector4 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Color value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(int uniformName, in Color value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Matrix3x2 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Matrix4 value, bool transpose=true)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value, transpose);
                }
            }

            public void SetUniformMaybe(int uniformName, in Matrix3x2 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Vector2 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Vector2i value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(int uniformName, in Vector2 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, int value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    GL.Uniform1(slot, value);
                    _clyde.CheckGlError();
                }
            }

            public void SetUniformMaybe(string uniformName, float value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    GL.Uniform1(slot, value);
                    _clyde.CheckGlError();
                }
            }

            private void ClearCaches()
            {
                _uniformCache.Clear();
                for (var i = 0; i < UniCount; i++)
                {
                    _uniformIntCache[i] = null;
                }
            }

            private static void ThrowCouldNotGetUniform(string name)
            {
                throw new ArgumentException($"Could not get uniform \"{name}\"!");
            }
        }
    }
}
