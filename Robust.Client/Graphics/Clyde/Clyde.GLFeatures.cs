using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Log;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // OpenGL feature detection go here.

        private bool _hasGLKhrDebug;
        // As per the extension specification, when implemented as extension in an ES context,
        // function names have to be suffixed by "KHR"
        // This keeps track of whether that's necessary.
        private bool _isGLKhrDebugESExtension;
        private bool _hasGLTextureSwizzle;
        private bool _hasGLSamplerObjects;
        private bool _hasGLSrgb;
        private bool _hasGLPrimitiveRestart;
        private bool _hasGLReadFramebuffer;
        private bool _hasGLUniformBuffers;
        private bool HasGLAnyVertexArrayObjects => _hasGLVertexArrayObject || _hasGLVertexArrayObjectOes;
        private bool _hasGLVertexArrayObject;
        private bool _hasGLVertexArrayObjectOes;
        private bool _hasGLFloatFramebuffers;
        private bool HasGLAnyMapBuffer => _hasGLMapBuffer || _hasGLMapBufferRange || _hasGLMapBufferOes;
        private bool _hasGLMapBuffer;
        private bool _hasGLMapBufferOes;
        private bool _hasGLMapBufferRange;
        private bool _hasGLPixelBufferObjects;
        private bool _hasGLStandardDerivatives;

        private bool _hasGLFenceSync;

        // These are set from Clyde.Windowing.
        private bool _isGLES;
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

            Logger.DebugS("clyde.ogl", "OpenGL capabilities:");

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
            }
            else
            {
                // OpenGL ES capabilities.
                CheckGLCap(ref _hasGLKhrDebug, "khr_debug", (3, 2), "GL_KHR_debug");
                if (!CompareVersion(3, 2, major, minor))
                {
                    // We're ES <3.2, KHR_debug is extension and needs KHR suffixes.
                    _isGLKhrDebugESExtension = true;
                    Logger.DebugS("clyde.ogl", "  khr_debug is ES extension!");
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
            }

            // TODO: Enable these on ES 3.0
            _hasGLSrgb = !_isGLES;
            _hasGLReadFramebuffer = !_isGLES;
            _hasGLPrimitiveRestart = !_isGLES;
            _hasGLUniformBuffers = !_isGLES;
            // This is 3.2 or extensions
            _hasGLFloatFramebuffers = !_isGLES;

            Logger.DebugS("clyde.ogl", $"  GLES: {_isGLES}");

            void CheckGLCap(ref bool cap, string capName, (int major, int minor)? versionMin = null,
                params string[] exts)
            {
                var (majorMin, minorMin) = versionMin ?? (int.MaxValue, int.MaxValue);
                // Check if feature is available from the GL context.
                cap = CompareVersion(majorMin, minorMin, major, minor) || extensions.Overlaps(exts);

                var prev = cap;
                var cVarName = $"display.ogl_block_{capName}";
                var block = ConfigurationManager.GetCVar<bool>(cVarName);

                if (block)
                {
                    cap = false;
                    Logger.DebugS("clyde.ogl", $"  {cVarName} SET, BLOCKING {capName} (was: {prev})");
                }

                Logger.DebugS("clyde.ogl", $"  {capName}: {cap}");
            }
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
                "standard_derivatives"
            };

            foreach (var cvar in cvars)
            {
                ConfigurationManager.RegisterCVar($"display.ogl_block_{cvar}", false);
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
                Logger.DebugS("clyde.ogl", "OpenGL Extensions: {0}", extensionsText);
                return extensions;
            }
            else
            {
                // GLES uses the (old?) API
                var extensions = GL.GetString(StringName.Extensions);
                Logger.DebugS("clyde.ogl", "OpenGL Extensions: {0}", extensions);
                return new HashSet<string>(extensions.Split(' '));
            }
        }
    }
}
