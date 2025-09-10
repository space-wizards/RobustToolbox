using System;
using System.Runtime.InteropServices;
using Robust.Shared;
#if WINDOWS
using TerraFX.Interop.Windows;
using TerraFX.Interop.DirectX;
#endif

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private GLContextBase? _glContext;

        // Current OpenGL version we managed to initialize with.
        private RendererOpenGLVersion _openGLVersion;

        private void InitGLContextManager()
        {
            CheckForceCompatMode();

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

        private void CheckForceCompatMode()
        {
#if WINDOWS
            // Qualcomm (Snapdragon/Adreno) devices have broken OpenGL drivers on Windows.

            if (CheckIsQualcommDevice())
            {
                _sawmillOgl.Info("We appear to be on a Qualcomm device. Enabling compat mode due to broken OpenGL driver");
                _cfg.OverrideDefault(CVars.DisplayCompat, true);
            }
#endif
        }

#if WINDOWS
        private static unsafe bool CheckIsQualcommDevice()
        {
            // Ideally we would check the OpenGL driver instead... but OpenGL is terrible so that's impossible.
            // Let's just check with DXGI instead.

            IDXGIFactory1* dxgiFactory;
            ThrowIfFailed(
                nameof(DirectX.CreateDXGIFactory1),
                DirectX.CreateDXGIFactory1(Windows.__uuidof<IDXGIFactory1>(), (void**) &dxgiFactory));

            try
            {
                uint idx = 0;
                IDXGIAdapter* adapter;
                while (dxgiFactory->EnumAdapters(idx, &adapter) != DXGI.DXGI_ERROR_NOT_FOUND)
                {
                    try
                    {
                        DXGI_ADAPTER_DESC desc;
                        ThrowIfFailed("GetDesc", adapter->GetDesc(&desc));

                        var descString = ((ReadOnlySpan<char>)desc.Description).TrimEnd('\0');
                        if (descString.Contains("qualcomm", StringComparison.OrdinalIgnoreCase) ||
                            descString.Contains("snapdragon", StringComparison.OrdinalIgnoreCase) ||
                            descString.Contains("adreno", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        adapter->Release();
                    }

                    idx += 1;
                }
            }
            finally
            {
                dxgiFactory->Release();
            }

            return false;
        }

        private static void ThrowIfFailed(string methodName, HRESULT hr)
        {
            if (Windows.FAILED(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
#endif

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
