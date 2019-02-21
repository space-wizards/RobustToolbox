using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics.Clyde
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
            private int _handle = -1;
            private Shader _fragmentShader;
            private Shader _vertexShader;
            public string Name { get; }
            private readonly Clyde _clyde;

            public ShaderProgram(Clyde clyde, string name=null)
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
                _handle = GL.CreateProgram();
                if (Name != null)
                {
                    _clyde._objectLabelMaybe(ObjectLabelIdentifier.Program, _handle, Name);
                }

                if (_vertexShader != null)
                {
                    GL.AttachShader(_handle, _vertexShader.Handle);
                }

                if (_fragmentShader != null)
                {
                    GL.AttachShader(_handle, _fragmentShader.Handle);
                }

                GL.LinkProgram(_handle);

                GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out var compiled);
                if (compiled != 1)
                {
                    throw new ShaderCompilationException(GL.GetProgramInfoLog(_handle));
                }
            }

            public void Use()
            {
                DebugTools.Assert(_handle != -1);

                if (_clyde._currentProgram == this)
                {
                    return;
                }

                _clyde._currentProgram = this;
                GL.UseProgram(_handle);
            }

            public void Delete()
            {
                if (_handle == -1)
                {
                    return;
                }

                GL.DeleteProgram(_handle);
                _handle = -1;
            }

            public int GetUniform(string name)
            {
                DebugTools.Assert(_handle != -1);

                if (_uniformCache.TryGetValue(name, out var result))
                {
                    return result;
                }

                result = GL.GetUniformLocation(_handle, name);
                if (result == -1)
                {
                    throw new ArgumentException("Could not get uniform!");
                }

                _uniformCache.Add(name, result);
                return result;
            }

            public void BindBlock(string blockName, int blockBinding)
            {
                var index = GL.GetUniformBlockIndex(_handle, blockName);
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
                        GL.UniformMatrix3(uniformId, 1, true, (float*)ptr);
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
                        GL.UniformMatrix4(uniformId, 1, true, (float*)ptr);
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
                        GL.Uniform4(uniformId, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Color color)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Color* ptr = &color)
                    {
                        GL.Uniform4(uniformId, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniform(string uniformName, in Vector3 vector)
            {
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Vector3* ptr = &vector)
                    {
                        GL.Uniform3(uniformId, 1, (float*)ptr);
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
                        GL.Uniform2(uniformId, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
            {
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, textureUnit - TextureUnit.Texture0);
            }
        }
    }
}
