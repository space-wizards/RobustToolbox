using System;
using System.Collections.Generic;
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
                _uniformCache.Clear();
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
                _uniformCache.Clear();
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
                    throw new ArgumentException("Could not get uniform!");
                }

                return result;
            }

            public bool TryGetUniform(string name, out int index)
            {
                DebugTools.Assert(_handle != 0);

                if (_uniformCache.TryGetValue(name, out index))
                {
                    return true;
                }

                index = GL.GetUniformLocation(_handle, name);
                _uniformCache.Add(name, index);
                return index != -1;
            }

            public bool HasUniform(string name) => TryGetUniform(name, out _);

            public void BindBlock(string blockName, uint blockBinding)
            {
                var index = (uint)GL.GetUniformBlockIndex(_handle, blockName);
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
                unsafe
                {
                    fixed (Matrix3* ptr = &matrix)
                    {
                        GL.UniformMatrix3(uniformId, 1, true, (float*) ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Matrix4 matrix)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Matrix4* ptr = &matrix)
                    {
                        GL.UniformMatrix4(uniformId, 1, true, (float*) ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Vector4 vector)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Vector4* ptr = &vector)
                    {
                        GL.Uniform4(uniformId, 1, (float*) ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Color color, bool convertToLinear=true)
            {
                var uniformId = GetUniform(uniformName);
                var converted = color;
                if (convertToLinear)
                {
                    converted = Color.FromSrgb(color);
                }

                unsafe
                {
                    GL.Uniform4(uniformId, 1, (float*) &converted);
                }
            }

            public void SetUniform(string uniformName, in Vector3 vector)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Vector3* ptr = &vector)
                    {
                        GL.Uniform3(uniformId, 1, (float*) ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Vector2 vector)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Vector2* ptr = &vector)
                    {
                        GL.Uniform2(uniformId, 1, (float*) ptr);
                    }
                }
            }

            public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, textureUnit - TextureUnit.Texture0);
            }

            public void SetUniformTextureMaybe(string uniformName, TextureUnit textureUnit)
            {
                if (HasUniform(uniformName))
                {
                    SetUniformTexture(uniformName, textureUnit);
                }
            }

            public void SetUniformMaybe(string uniformName, in Vector4 value)
            {
                if (HasUniform(uniformName))
                {
                    SetUniform(uniformName, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Color value)
            {
                if (HasUniform(uniformName))
                {
                    SetUniform(uniformName, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Matrix3 value)
            {
                if (HasUniform(uniformName))
                {
                    SetUniform(uniformName, value);
                }
            }

            public void SetUniformMaybe(string uniformName, in Vector2 value)
            {
                if (HasUniform(uniformName))
                {
                    SetUniform(uniformName, value);
                }
            }
        }
    }
}
