using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.Generic;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Wrapper for pixel shaders
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Shader : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Special type that can be passed to SetParameter,
            /// and that represents the texture of the object being drawn
            /// </summary>
            ////////////////////////////////////////////////////////////
            public class CurrentTextureType {}

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Special value that can be passed to SetParameter,
            /// and that represents the texture of the object being drawn
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static readonly CurrentTextureType CurrentTexture = null;

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Load the vertex and fragment shaders from files
            ///
            /// This function can load both the vertex and the fragment
            /// shaders, or only one of them: pass NULL if you don't want to load
            /// either the vertex shader or the fragment shader.
            /// The sources must be text files containing valid shaders
            /// in GLSL language. GLSL is a C-like language dedicated to
            /// OpenGL shaders; you'll probably need to read a good documentation
            /// for it before writing your own shaders.
            /// </summary>
            /// <param name="vertexShaderFilename">Path of the vertex shader file to load, or null to skip this shader</param>
            /// <param name="fragmentShaderFilename">Path of the fragment shader file to load, or null to skip this shader</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Shader(string vertexShaderFilename, string fragmentShaderFilename) :
                base(sfShader_createFromFile(vertexShaderFilename, fragmentShaderFilename))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("shader", vertexShaderFilename + " " + fragmentShaderFilename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Load both the vertex and fragment shaders from custom streams
            ///
            /// This function can load both the vertex and the fragment
            /// shaders, or only one of them: pass NULL if you don't want to load
            /// either the vertex shader or the fragment shader.
            /// The sources must be valid shaders in GLSL language. GLSL is
            /// a C-like language dedicated to OpenGL shaders; you'll
            /// probably need to read a good documentation for it before
            /// writing your own shaders.
            /// </summary>
            /// <param name="vertexShaderStream">Source stream to read the vertex shader from, or null to skip this shader</param>
            /// <param name="fragmentShaderStream">Source stream to read the fragment shader from, or null to skip this shader</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Shader(Stream vertexShaderStream, Stream fragmentShaderStream) :
                base(IntPtr.Zero)
            {
                StreamAdaptor vertexAdaptor = new StreamAdaptor(vertexShaderStream);
                StreamAdaptor fragmentAdaptor = new StreamAdaptor(fragmentShaderStream);
                SetThis(sfShader_createFromStream(vertexAdaptor.InputStreamPtr, fragmentAdaptor.InputStreamPtr));
                vertexAdaptor.Dispose();
                fragmentAdaptor.Dispose();

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("shader");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Load both the vertex and fragment shaders from source codes in memory
            ///
            /// This function can load both the vertex and the fragment
            /// shaders, or only one of them: pass NULL if you don't want to load
            /// either the vertex shader or the fragment shader.
            /// The sources must be valid shaders in GLSL language. GLSL is
            /// a C-like language dedicated to OpenGL shaders; you'll
            /// probably need to read a good documentation for it before
            /// writing your own shaders.
            /// </summary>
            /// <param name="vertexShader">String containing the source code of the vertex shader</param>
            /// <param name="fragmentShader">String containing the source code of the fragment shader</param>
            /// <returns>New shader instance</returns>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public static Shader FromString(string vertexShader, string fragmentShader)
            {
                IntPtr ptr = sfShader_createFromMemory(vertexShader, fragmentShader);
                if (ptr == IntPtr.Zero)
                    throw new LoadingFailedException("shader");

                return new Shader(ptr);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a float parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a float
            /// (float GLSL type).
            /// </summary>
            ///
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="x">Value to assign</param>
            ///
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, float x)
            {
                sfShader_setFloatParameter(CPointer, name, x);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a 2-components vector parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 2x1 vector
            /// (vec2 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="x">First component of the value to assign</param>
            /// <param name="y">Second component of the value to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, float x, float y)
            {
                sfShader_setFloat2Parameter(CPointer, name, x, y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a 3-components vector parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 3x1 vector
            /// (vec3 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="x">First component of the value to assign</param>
            /// <param name="y">Second component of the value to assign</param>
            /// <param name="z">Third component of the value to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, float x, float y, float z)
            {
                sfShader_setFloat3Parameter(CPointer, name, x, y, z);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a 4-components vector parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 4x1 vector
            /// (vec4 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="x">First component of the value to assign</param>
            /// <param name="y">Second component of the value to assign</param>
            /// <param name="z">Third component of the value to assign</param>
            /// <param name="w">Fourth component of the value to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, float x, float y, float z, float w)
            {
                sfShader_setFloat4Parameter(CPointer, name, x, y, z, w);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a 2-components vector parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 2x1 vector
            /// (vec2 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="vector">Vector to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, Vector2 vector)
            {
                SetParameter(name, vector.X, vector.Y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a color parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 4x1 vector
            /// (vec4 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="color">Color to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, Color color)
            {
                sfShader_setColorParameter(CPointer, name, color);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a matrix parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 4x4 matrix
            /// (mat4 GLSL type).
            /// </summary>
            /// <param name="name">Name of the parameter in the shader</param>
            /// <param name="transform">Transform to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, Transform transform)
            {
                sfShader_setTransformParameter(CPointer, name, transform);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a texture parameter of the shader
            ///
            /// "name" is the name of the variable to change in the shader.
            /// The corresponding parameter in the shader must be a 2D texture
            /// (sampler2D GLSL type).
            ///
            /// It is important to note that \a texture must remain alive as long
            /// as the shader uses it, no copy is made internally.
            ///
            /// To use the texture of the object being draw, which cannot be
            /// known in advance, you can pass the special value
            /// Shader.CurrentTexture.
            /// </summary>
            /// <param name="name">Name of the texture in the shader</param>
            /// <param name="texture">Texture to assign</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, Texture texture)
            {
                myTextures[name] = texture;
                sfShader_setTextureParameter(CPointer, name, texture.CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change a texture parameter of the shader
            ///
            /// This overload maps a shader texture variable to the
            /// texture of the object being drawn, which cannot be
            /// known in advance. The second argument must be
            /// sf::Shader::CurrentTexture.
            /// The corresponding parameter in the shader must be a 2D texture
            /// (sampler2D GLSL type).
            /// </summary>
            /// <param name="name">Name of the texture in the shader</param>
            /// <param name="current">Always pass the spacial value Shader.CurrentTexture</param>
            ////////////////////////////////////////////////////////////
            public void SetParameter(string name, CurrentTextureType current)
            {
                sfShader_setCurrentTextureParameter(CPointer, name);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Bind the shader for rendering (activate it)
            ///
            /// This function is normally for internal use only, unless
            /// you want to use the shader with a custom OpenGL rendering
            /// instead of a SFML drawable.
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Bind()
            {
                sfShader_bind(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Unbind the shader (deactivate it)
            ///
            /// This function is normally for internal use only, unless
            /// you want to use the shader with a custom OpenGL rendering
            /// instead of a SFML drawable.
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Unbind()
            {
                sfShader_unbind(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Tell whether or not the system supports shaders.
            ///
            /// This property should always be checked before using
            /// the shader features. If it returns false, then
            /// any attempt to use Shader will fail.
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static bool IsAvailable
            {
                get {return sfShader_isAvailable();}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Shader]";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                if (!disposing)
                    Context.Global.SetActive(true);

                myTextures.Clear();
                sfShader_destroy(CPointer);

                if (!disposing)
                    Context.Global.SetActive(false);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the shader from a pointer
            /// </summary>
            /// <param name="ptr">Pointer to the shader instance</param>
            ////////////////////////////////////////////////////////////
            public Shader(IntPtr ptr) :
                base(ptr)
            {
            }

            Dictionary<string, Texture> myTextures = new Dictionary<string, Texture>();

            #region Imports

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfShader_createFromFile(string vertexShaderFilename, string fragmentShaderFilename);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfShader_createFromMemory(string vertexShader, string fragmentShader);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfShader_createFromStream(IntPtr vertexShaderStream, IntPtr fragmentShaderStream);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_destroy(IntPtr shader);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setFloatParameter(IntPtr shader, string name, float x);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setFloat2Parameter(IntPtr shader, string name, float x, float y);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setFloat3Parameter(IntPtr shader, string name, float x, float y, float z);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setFloat4Parameter(IntPtr shader, string name, float x, float y, float z, float w);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setColorParameter(IntPtr shader, string name, Color color);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setTransformParameter(IntPtr shader, string name, Transform transform);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setTextureParameter(IntPtr shader, string name, IntPtr texture);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_setCurrentTextureParameter(IntPtr shader, string name);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_bind(IntPtr shader);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShader_unbind(IntPtr shader);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfShader_isAvailable();

            #endregion
        }
    }
}
