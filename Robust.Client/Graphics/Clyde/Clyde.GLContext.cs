﻿using System;
using Robust.Shared;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private GLContextBase? _glContext;

        // Current OpenGL version we managed to initialize with.
        private RendererOpenGLVersion _openGLVersion;

        private void InitGLContextManager()
        {
            // Advanced GL contexts currently disabled due to lack of testing etc.
            if (OperatingSystem.IsWindows() && _cfg.GetCVar(CVars.DisplayAngle))
            {
                if (_cfg.GetCVar(CVars.DisplayAngleCustomSwapChain))
                {
                    _sawmillOgl.Debug("Trying custom swap chain ANGLE.");
                    var ctxAngle = new GLContextAngle(this);

                    if (ctxAngle.TryInitialize())
                    {
                        _sawmillOgl.Debug("Successfully initialized custom ANGLE");
                        _glContext = ctxAngle;

                        ctxAngle.EarlyInit();
                        return;
                    }
                }

                if (_cfg.GetCVar(CVars.DisplayEgl))
                {
                    _sawmillOgl.Debug("Trying EGL");
                    var ctxEgl = new GLContextEgl(this);
                    ctxEgl.InitializePublic();
                    _glContext = ctxEgl;
                    return;
                }
            }

            /*
            if (OperatingSystem.IsLinux() && _cfg.GetCVar(CVars.DisplayEgl))
            {
                _sawmillOgl.Debug("Trying EGL");
                var ctxEgl = new GLContextEgl(this);
                ctxEgl.InitializePublic();
                _glContext = ctxEgl;
                return;
            }
            */

            _glContext = new GLContextWindow(this);
        }

        private struct GLContextSpec
        {
            public int Major;
            public int Minor;
            public GLContextProfile Profile;
            public GLContextCreationApi CreationApi;
            // Used by GLContextWindow to figure out which GL version managed to initialize.
            public RendererOpenGLVersion OpenGLVersion;
        }

        private enum GLContextProfile
        {
            Compatibility,
            Core,
            Es
        }

        private enum GLContextCreationApi
        {
            Native,
            Egl,
        }
    }
}
