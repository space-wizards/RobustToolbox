using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OpenToolkit.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // OpenGL feature detection go here.

        private bool _hasGLKhrDebug;
        private bool _glDebuggerPresent;

        // As per the extension specification, when implemented as extension in an ES context,
        // function names have to be suffixed by "KHR"
        // This keeps track of whether that's necessary.
        private bool _isGLKhrDebugESExtension;
        private bool _hasGLTextureSwizzle;
        private bool _hasGLSamplerObjects;
        private bool _hasGLSrgb;
        private bool _hasGLPrimitiveRestart;
        private bool _hasGLPrimitiveRestartFixedIndex;
        private bool _hasGLReadFramebuffer;
        private bool _hasGLUniformBuffers;
        private bool HasGLAnyVertexArrayObjects => _hasGLVertexArrayObject || _hasGLVertexArrayObjectOes;
        private bool _hasGLVertexArrayObject;
        private bool _hasGLVertexArrayObjectOes;
        private bool _hasGLFloatFramebuffers;
        private bool _hasGLES3Shaders;
        private bool HasGLAnyMapBuffer => _hasGLMapBuffer || _hasGLMapBufferRange || _hasGLMapBufferOes;
        private bool _hasGLMapBuffer;
        private bool _hasGLMapBufferOes;
        private bool _hasGLMapBufferRange;
        private bool _hasGLPixelBufferObjects;
        private bool _hasGLStandardDerivatives;

        private bool _hasGLFenceSync;

        // These are set from Clyde.Windowing.
        private bool _isGLES;
        private bool _isGLES2;
        private bool _isCore;

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void LoadVendorSettings(string vendor, string renderer, string version)
        {
            // Nothing yet.
        }

        private static bool CompareVersion(int majorA, int minorA, int majorB, int minorB)
        {
            if (majorB > majorA)
            {
                return true;
            }

            return majorA == majorB && minorB >= minorA;
        }

        private void DetectOpenGLFeatures(int major, int minor)
        {
            var extensions = GetGLExtensions();

            CheckGLDebuggerStatus(extensions);

            _sawmillOgl.Debug("OpenGL capabilities:");

            if (!_isGLES)
            {
                // Desktop OpenGL capabilities.
                CheckGLCap(ref _hasGLKhrDebug, "khr_debug", (4, 2), "GL_KHR_debug");
                CheckGLCap(ref _hasGLSamplerObjects, "sampler_objects", (3, 3), "GL_ARB_sampler_objects");
                CheckGLCap(ref _hasGLTextureSwizzle, "texture_swizzle", (3, 3), "GL_ARB_texture_swizzle",
                    "GL_EXT_texture_swizzle");
                CheckGLCap(ref _hasGLVertexArrayObject, "vertex_array_object", (3, 0), "GL_ARB_vertex_array_object");
                CheckGLCap(ref _hasGLFenceSync, "fence_sync", (3, 2), "GL_ARB_sync");
                CheckGLCap(ref _hasGLMapBuffer, "map_buffer", (2, 0));
                CheckGLCap(ref _hasGLMapBufferRange, "map_buffer_range", (3, 0));
                CheckGLCap(ref _hasGLPixelBufferObjects, "pixel_buffer_object", (2, 1));
                CheckGLCap(ref _hasGLStandardDerivatives, "standard_derivatives", (2, 1));

                _hasGLSrgb = true;
                _hasGLReadFramebuffer = true;
                _hasGLPrimitiveRestart = true;
                _hasGLUniformBuffers = true;
                _hasGLFloatFramebuffers = true;
            }
            else
            {
                // OpenGL ES capabilities.
                CheckGLCap(ref _hasGLKhrDebug, "khr_debug", (3, 2), "GL_KHR_debug");
                if (!CompareVersion(3, 2, major, minor))
                {
                    // We're ES <3.2, KHR_debug is extension and needs KHR suffixes.
                    _isGLKhrDebugESExtension = true;
                    _sawmillOgl.Debug("  khr_debug is ES extension!");
                }

                CheckGLCap(ref _hasGLVertexArrayObject, "vertex_array_object", (3, 0));
                CheckGLCap(ref _hasGLVertexArrayObjectOes, "vertex_array_object_oes",
                    exts: "GL_OES_vertex_array_object");
                CheckGLCap(ref _hasGLTextureSwizzle, "texture_swizzle", (3, 0));
                CheckGLCap(ref _hasGLFenceSync, "fence_sync", (3, 0));
                // ReSharper disable once StringLiteralTypo
                CheckGLCap(ref _hasGLMapBufferOes, "map_buffer_oes", exts: "GL_OES_mapbuffer");
                CheckGLCap(ref _hasGLMapBufferRange, "map_buffer_range", (3, 0));
                CheckGLCap(ref _hasGLPixelBufferObjects, "pixel_buffer_object", (3, 0));
                CheckGLCap(ref _hasGLStandardDerivatives, "standard_derivatives", (3, 0), "GL_OES_standard_derivatives");
                CheckGLCap(ref _hasGLReadFramebuffer, "read_framebuffer", (3, 0));
                CheckGLCap(ref _hasGLPrimitiveRestartFixedIndex, "primitive_restart", (3, 0));
                CheckGLCap(ref _hasGLUniformBuffers, "uniform_buffers", (3, 0));
                CheckGLCap(ref _hasGLFloatFramebuffers, "float_framebuffers", (3, 2), "GL_EXT_color_buffer_float");
                CheckGLCap(ref _hasGLES3Shaders, "gles3_shaders", (3, 0));

                if (major >= 3)
                {
                    if (_glContext!.HasBrokenWindowSrgb)
                    {
                        _hasGLSrgb = false;
                        _sawmillOgl.Debug("  sRGB: false (window broken sRGB)");
                    }
                    else
                    {
                        _hasGLSrgb = true;
                        _sawmillOgl.Debug("  sRGB: true");
                    }
                }
                else
                {
                    _hasGLSrgb = false;
                    _sawmillOgl.Debug("  sRGB: false");
                }
            }

            _sawmillOgl.Debug($"  GLES: {_isGLES}");

            void CheckGLCap(ref bool cap, string capName, (int major, int minor)? versionMin = null,
                params string[] exts)
            {
                var (majorMin, minorMin) = versionMin ?? (int.MaxValue, int.MaxValue);
                // Check if feature is available from the GL context.
                cap = CompareVersion(majorMin, minorMin, major, minor) || extensions.Overlaps(exts);

                var prev = cap;
                var cVarName = $"display.ogl_block_{capName}";
                var block = _cfg.GetCVar<bool>(cVarName);

                if (block)
                {
                    cap = false;
                    _sawmillOgl.Debug($"  {cVarName} SET, BLOCKING {capName} (was: {prev})");
                }

                _sawmillOgl.Debug($"  {capName}: {cap}");
            }
        }

        private void CheckGLDebuggerStatus(HashSet<string> extensions)
        {
            if (!extensions.Contains("GL_EXT_debug_tool"))
                return;

            const int GL_DEBUG_TOOL_EXT = 0x6789;
            const int GL_DEBUG_TOOL_NAME_EXT = 0x678A;

            _glDebuggerPresent = GL.IsEnabled((EnableCap)GL_DEBUG_TOOL_EXT);
            var name = GL.GetString((StringName)GL_DEBUG_TOOL_NAME_EXT);
            _sawmillOgl.Debug($"OpenGL debugger present: {name}");
        }

        private void RegisterBlockCVars()
        {
            string[] cvars =
            {
                "khr_debug",
                "sampler_objects",
                "texture_swizzle",
                "vertex_array_object",
                "vertex_array_object_oes",
                "fence_sync",
                "map_buffer",
                "map_buffer_range",
                "pixel_buffer_object",
                "map_buffer_oes",
                "standard_derivatives",
                "read_framebuffer",
                "primitive_restart",
                "uniform_buffers",
                "float_framebuffers",
                "gles3_shaders",
            };

            foreach (var cvar in cvars)
            {
                _cfg.RegisterCVar($"display.ogl_block_{cvar}", false);
            }
        }

        private HashSet<string> GetGLExtensions()
        {
            if (!_isGLES)
            {
                var extensions = new HashSet<string>();
                var extensionsText = "";
                // Desktop OpenGL uses this API to discourage static buffers
                var count = GL.GetInteger(GetPName.NumExtensions);
                for (var i = 0; i < count; i++)
                {
                    if (i != 0)
                    {
                        extensionsText += " ";
                    }
                    var extension = GL.GetString(StringNameIndexed.Extensions, i);
                    extensionsText += extension;
                    extensions.Add(extension);
                }
                _sawmillOgl.Debug("OpenGL Extensions: {0}", extensionsText);
                return extensions;
            }
            else
            {
                // GLES uses the (old?) API
                var extensions = GL.GetString(StringName.Extensions);
                _sawmillOgl.Debug("OpenGL Extensions: {0}", extensions);
                return new HashSet<string>(extensions.Split(' '));
            }
        }
    }
}
