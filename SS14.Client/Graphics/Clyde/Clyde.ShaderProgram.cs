using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private class ShaderProgram
        {
            private readonly Dictionary<string, int> _uniformCache = new Dictionary<string, int>();
            private int _handle = -1;
            private Shader _fragmentShader;
            private Shader _vertexShader;

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

            public void Compile(bool deleteShaders=true)
            {
                _uniformCache.Clear();
                _handle = GL.CreateProgram();

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
                    throw new Exception(GL.GetProgramInfoLog(_handle));
                }

                if (!deleteShaders)
                {
                    return;
                }

                // don't need the shaders anymore, they are compiled into the program
                _vertexShader?.Delete();
                _fragmentShader?.Delete();
            }

            public void Use()
            {
                DebugTools.Assert(_handle != -1);

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

            public void SetUniform(string uniformName, in Matrix4 matrix)
            {
                Use();
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
                Use();
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
                Use();
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
                Use();
                var uniformId = GetUniform(uniformName);
                unsafe
                {
                    fixed (Vector3* ptr = &vector)
                    {
                        GL.Uniform3(uniformId, 1, (float*)ptr);
                    }
                }
            }

            public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
            {
                Use();
                var uniformId = GetUniform(uniformName);
                GL.Uniform1(uniformId, textureUnit - TextureUnit.Texture0);
            }
        }
    }
}
