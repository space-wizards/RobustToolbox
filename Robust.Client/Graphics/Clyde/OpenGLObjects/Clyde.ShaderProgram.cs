using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     This is an utility class. It does not check whether the OpenGL state machine is set up correctly.
        ///     You've been warned:
        ///     using things like <see cref="SetUniformTexture" /> if this buffer isn't bound WILL mess things up!
        /// </summary>
        private class ShaderProgram
        {
            private readonly sbyte?[] _uniformIntCache = new sbyte?[Clyde.UniCount];
            private readonly Dictionary<string, int> _uniformCache = new Dictionary<string, int>();
            private uint _handle = 0;
            private Shader _fragmentShader;
            private Shader _vertexShader;
            public string Name { get; }
            private readonly Clyde _clyde;

            public ShaderProgram(Clyde clyde, string name = null)
            {
                _clyde = clyde;
                Name = name;
            }

            public void Add(Shader shader)
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

            public void Link()
            {
                ClearCaches();
                _handle = (uint) GL.CreateProgram();
                if (Name != null)
                {
                    _clyde._objectLabelMaybe(ObjectLabelIdentifier.Program, _handle, Name);
                }

                if (_vertexShader != null)
                {
                    GL.AttachShader(_handle, _vertexShader.ObjectHandle);
                }

                if (_fragmentShader != null)
                {
                    GL.AttachShader(_handle, _fragmentShader.ObjectHandle);
                }

                GL.LinkProgram(_handle);

                GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out var compiled);
                if (compiled != 1)
                {
                    throw new ShaderCompilationException(GL.GetProgramInfoLog((int) _handle));
                }
            }

            public void Use()
            {
                DebugTools.Assert(_handle != 0);

                if (_clyde._currentProgram == this)
                {
                    return;
                }

                _clyde._currentProgram = this;
                GL.UseProgram(_handle);
            }

            public void Delete()
            {
                if (_handle == 0)
                {
                    return;
                }

                GL.DeleteProgram(_handle);
                _handle = 0;
            }

            public int GetUniform(string name)
            {
                if (!TryGetUniform(name, out var result))
                {
                    ThrowCouldNotGetUniform();
                }

                return result;
            }

            public int GetUniform(int id)
            {
                if (!TryGetUniform(id, out var result))
                {
                    ThrowCouldNotGetUniform();
                }

                return result;
            }

            public bool TryGetUniform(string name, out int index)
            {
                DebugTools.Assert(_handle != 0);

                if (_uniformCache.TryGetValue(name, out index))
                {
                    return index != -1;
                }

                index = GL.GetUniformLocation(_handle, name);
                _uniformCache.Add(name, index);
                return index != -1;
            }

            public bool TryGetUniform(int id, out int index)
            {
                DebugTools.Assert(_handle != 0);
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
                    case UniIModulate:
                        name = UniModulate;
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

                index = GL.GetUniformLocation(_handle, name);
                _uniformIntCache[id] = (sbyte)index;
                return index != -1;
            }

            public bool HasUniform(string name) => TryGetUniform(name, out _);
            public bool HasUniform(int id) => TryGetUniform(id, out _);

            public void BindBlock(string blockName, uint blockBinding)
            {
                var index = (uint) GL.GetUniformBlockIndex(_handle, blockName);
                GL.UniformBlockBinding(_handle, index, blockBinding);
            }

            public void SetUniform(string uniformName, int integer)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, integer);
            }

            public void SetUniform(string uniformName, float single)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, single);
            }

            public void SetUniform(string uniformName, in Matrix3 matrix)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix);
            }

            public void SetUniform(int uniformName, in Matrix3 matrix)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void SetUniformDirect(int slot, in Matrix3 value)
            {
                fixed (Matrix3* ptr = &value)
                {
                    GL.UniformMatrix3(slot, 1, true, (float*) ptr);
                }
            }

            public void SetUniform(string uniformName, in Matrix4 matrix)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, matrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void SetUniformDirect(int uniformId, in Matrix4 value)
            {
                fixed (Matrix4* ptr = &value)
                {
                    GL.UniformMatrix4(uniformId, 1, true, (float*) ptr);
                }
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
            private static void SetUniformDirect(int slot, in Vector4 vector)
            {
                unsafe
                {
                    fixed (Vector4* ptr = &vector)
                    {
                        GL.Uniform4(slot, 1, (float*)ptr);
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
            private static void SetUniformDirect(int slot, in Color color, bool convertToLinear=true)
            {
                var converted = color;
                if (convertToLinear)
                {
                    converted = Color.FromSrgb(color);
                }

                unsafe
                {
                    GL.Uniform4(slot, 1, (float*) &converted);
                }
            }

            public void SetUniform(string uniformName, in Vector3 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SetUniformDirect(int slot, in Vector3 vector)
            {
                unsafe
                {
                    fixed (Vector3* ptr = &vector)
                    {
                        GL.Uniform3(slot, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Vector2 vector)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformDirect(uniformId, vector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SetUniformDirect(int slot, in Vector2 vector)
            {
                unsafe
                {
                    fixed (Vector2* ptr = &vector)
                    {
                        GL.Uniform2(slot, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
            {
                var uniformId = GetUniform(uniformName);
                SetUniformTextureDirect(uniformId, textureUnit);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SetUniformTextureDirect(int slot, TextureUnit value)
            {
                GL.Uniform1(slot, value - TextureUnit.Texture0);
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

            public void SetUniformMaybe(string uniformName, in Matrix3 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(int uniformName, in Matrix3 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Matrix4 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
                }
            }

            public void SetUniformMaybe(int uniformName, in Matrix4 value)
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

            public void SetUniformMaybe(int uniformName, in Vector2 value)
            {
                if (TryGetUniform(uniformName, out var slot))
                {
                    SetUniformDirect(slot, value);
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

            private static void ThrowCouldNotGetUniform()
            {
                throw new ArgumentException("Could not get uniform!");
            }
        }
    }
}
