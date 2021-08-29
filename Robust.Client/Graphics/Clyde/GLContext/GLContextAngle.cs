using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using TerraFX.Interop;
using static Robust.Client.Graphics.Clyde.Egl;
using static TerraFX.Interop.D3D_DRIVER_TYPE;
using static TerraFX.Interop.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.DXGI_SWAP_EFFECT;
using static TerraFX.Interop.Windows;


namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     Explicit ANGLE GL context with manual DXGI/D3D device and swap chain management.
        /// </summary>
        private sealed unsafe class GLContextAngle : GLContextBase
        {
            // Thanks to mpv's implementation of context_angle for inspiration/hints.
            // https://github.com/mpv-player/mpv/blob/f8e62d3d82dd0a3d06f9a557d756f0ad78118cc7/video/out/opengl/context_angle.c

            // NOTE: This class only handles GLES3/D3D11.
            // For anything lower we just let ANGLE fall back and do the work 100%.

            private IDXGIFactory1* _factory;
            private IDXGIAdapter1* _adapter;
            private ID3D11Device* _device;
            private ID3D11DeviceContext* _deviceContext;
            private void* _eglDevice;
            private void* _eglDisplay;
            private void* _eglContext;
            private void* _eglConfig;

            private uint _swapInterval;

            private readonly Dictionary<WindowId, WindowData> _windowData = new();

            public override bool GlesOnly => true;

            public GLContextAngle(Clyde clyde) : base(clyde)
            {
            }

            public override GLContextSpec? SpecWithOpenGLVersion(RendererOpenGLVersion version)
            {
                // Do not initialize GL context on the window directly, we use ANGLE.
                return null;
            }

            public override void UpdateVSync()
            {
                _swapInterval = (uint) (Clyde._vSync ? 1 : 0);
            }

            public override void WindowCreated(WindowReg reg)
            {
                var data = new WindowData
                {
                    Reg = reg
                };
                _windowData[reg.Id] = data;

                IDXGIFactory2* factory2;
                var iid = IID_IDXGIFactory2;
                if (FAILED(_factory->QueryInterface(&iid, (void**) &factory2)))
                    factory2 = null;

                var hWnd = Clyde._windowing!.WindowGetWin32Window(reg)!.Value;

                // todo: exception management.
                if (factory2 != null && !Clyde._cfg.GetCVar(CVars.DisplayAngleDxgi1))
                {
                    CreateSwapChain2(factory2, hWnd, data);
                }
                else
                {
                    CreateSwapChain1(data);
                }

                var rt = Clyde.RtToLoaded(reg.RenderTarget);
                rt.FlipY = true;

                if (factory2 != null)
                    factory2->Release();

                if (reg.IsMainWindow)
                {
                    UpdateVSync();
                    eglMakeCurrent(_eglDisplay, data.EglBackbuffer, data.EglBackbuffer, _eglContext);
                }
            }

            private void DestroyBackbuffer(WindowData data)
            {
                if (data.EglBackbuffer != null)
                {
                    eglMakeCurrent(_eglDisplay, null, null, null);
                    eglDestroySurface(_eglDisplay, data.EglBackbuffer);

                    data.EglBackbuffer = null;
                }

                data.Backbuffer->Release();
                data.Backbuffer = null;
            }

            private void SetupBackbuffer(WindowData data)
            {
                DebugTools.Assert(data.Backbuffer == null, "Backbuffer must have been released!");
                DebugTools.Assert(data.EglBackbuffer == null, "EGL Backbuffer must have been released!");

                fixed (ID3D11Texture2D** texPtr = &data.Backbuffer)
                {
                    var iid = IID_ID3D11Texture2D;
                    ThrowIfFailed("GetBuffer", data.SwapChain->GetBuffer(0, &iid, (void**) texPtr));
                }

                var attributes = stackalloc int[]
                {
                    EGL_TEXTURE_FORMAT, EGL_TEXTURE_RGBA,
                    EGL_TEXTURE_TARGET, EGL_TEXTURE_2D,
                    EGL_NONE
                };

                data.EglBackbuffer = eglCreatePbufferFromClientBuffer(
                    _eglDisplay,
                    EGL_D3D_TEXTURE_ANGLE,
                    data.Backbuffer,
                    _eglConfig,
                    attributes);
            }

            private void CreateSwapChain2(IDXGIFactory2* factory2, nint hWnd, WindowData data)
            {
                var desc = new DXGI_SWAP_CHAIN_DESC1
                {
                    Width = (uint) data.Reg.FramebufferSize.X,
                    Height = (uint) data.Reg.FramebufferSize.Y,
                    Format = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
                    SampleDesc =
                    {
                        Count = 1
                    },
                    BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT | DXGI_USAGE_SHADER_INPUT,
                    BufferCount = 2,
                    SwapEffect = DXGI_SWAP_EFFECT_DISCARD
                };

                IDXGISwapChain1* swapChain1;

                ThrowIfFailed("CreateSwapChainForHwnd", factory2->CreateSwapChainForHwnd(
                    (IUnknown*) _device,
                    hWnd,
                    &desc,
                    null,
                    null,
                    &swapChain1
                ));

                fixed (IDXGISwapChain** swapPtr = &data.SwapChain)
                {
                    var iid = IID_IDXGISwapChain;
                    ThrowIfFailed("QueryInterface", swapChain1->QueryInterface(&iid, (void**) swapPtr));
                }

                swapChain1->Release();

                SetupBackbuffer(data);
            }

            private void CreateSwapChain1(WindowData data)
            {
                throw new NotImplementedException();
            }

            public override void WindowDestroyed(WindowReg reg)
            {
                throw new NotImplementedException();
            }

            public bool TryInitialize()
            {
                try
                {
                    TryInitializeCore();
                }
                catch (Exception e)
                {
                    Logger.ErrorS("clyde.ogl.angle", $"Failed to initialize custom ANGLE: {e}");
                    Shutdown();
                    return false;
                }

                return true;
            }

            private void TryInitializeCore()
            {
                var extensions = Marshal.PtrToStringUTF8((nint) eglQueryString(null, EGL_EXTENSIONS));
                Logger.DebugS("clyde.ogl.angle", $"EGL client extensions: {extensions}!");

                CreateD3D11Device();
                CreateEglContext();
            }

            private void CreateEglContext()
            {
                _eglDevice = eglCreateDeviceANGLE(EGL_D3D11_DEVICE_ANGLE, _device, null);
                if (_eglDevice == (void*) EGL_NO_DEVICE_EXT)
                    throw new Exception("eglCreateDeviceANGLE failed.");

                _eglDisplay = eglGetPlatformDisplayEXT(EGL_PLATFORM_DEVICE_EXT, _eglDevice, null);
                if (_eglDisplay == null)
                    throw new Exception("eglGetPlatformDisplayEXT failed.");

                int major;
                int minor;
                if (eglInitialize(_eglDisplay, &major, &minor) == EGL_FALSE)
                    throw new Exception("eglInitialize failed.");

                var vendor = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VENDOR));
                var version = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VERSION));
                var extensions = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_EXTENSIONS));

                Logger.DebugS("clyde.ogl.angle", "EGL initialized!");
                Logger.DebugS("clyde.ogl.angle", $"EGL vendor: {vendor}!");
                Logger.DebugS("clyde.ogl.angle", $"EGL version: {version}!");
                Logger.DebugS("clyde.ogl.angle", $"EGL extensions: {extensions}!");

                if (eglBindAPI(EGL_OPENGL_ES_API) != EGL_TRUE)
                    throw new Exception("eglBindAPI failed.");

                var attribs = stackalloc int[]
                {
                    EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
                    EGL_RED_SIZE, 8,
                    EGL_GREEN_SIZE, 8,
                    EGL_BLUE_SIZE, 8,
                    EGL_ALPHA_SIZE, 8,
                    EGL_STENCIL_SIZE, 8,
                    EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT,
                    EGL_NONE
                };

                var numConfigs = 0;
                if (eglChooseConfig(_eglDisplay, attribs, null, 0, &numConfigs) == EGL_FALSE)
                    throw new Exception("eglChooseConfig failed.");

                var configs = stackalloc void*[numConfigs];
                if (eglChooseConfig(_eglDisplay, attribs, configs, numConfigs, &numConfigs) == EGL_FALSE)
                    throw new Exception("eglChooseConfig failed.");

                if (numConfigs == 0)
                    throw new Exception("No compatible EGL configurations returned!");

                Logger.DebugS("clyde.ogl.angle", $"{numConfigs} EGL configs possible!");

                for (var i = 0; i < numConfigs; i++)
                {
                    Logger.DebugS("clyde.ogl.angle", DumpEglConfig(_eglDisplay, configs[i]));
                }

                _eglConfig = configs[0];

                var createAttribs = stackalloc int[]
                {
                    EGL_CONTEXT_CLIENT_VERSION, 3,
                    EGL_NONE
                };

                _eglContext = eglCreateContext(_eglDisplay, _eglConfig, null, createAttribs);
                if (_eglContext == (void*) EGL_NO_CONTEXT)
                    throw new Exception("eglCreateContext failed!");

                Logger.DebugS("clyde.ogl.angle", "EGL context created!");
            }

            private void CreateD3D11Device()
            {
                IDXGIDevice1* dxgiDevice = null;

                try
                {
                    var iid = IID_IDXGIFactory1;

                    fixed (IDXGIFactory1** ptr = &_factory)
                    {
                        ThrowIfFailed(nameof(CreateDXGIFactory1), CreateDXGIFactory1(&iid, (void**) ptr));
                    }

                    // Try to find the correct adapter if specified.
                    var adapterName = Clyde._cfg.GetCVar(CVars.DisplayAdapter);

                    if (adapterName != "")
                    {
                        _adapter = TryFindAdapterWithName(adapterName);

                        if (_adapter == null)
                        {
                            Logger.WarningS("clyde.ogl.angle",
                                $"Unable to find display adapter with requested name: {adapterName}");
                        }

                        Logger.DebugS("clyde.ogl.angle", $"Found display adapter with name: {adapterName}");
                    }

                    var featureLevels = stackalloc D3D_FEATURE_LEVEL[]
                    {
                        D3D_FEATURE_LEVEL_11_1,
                        D3D_FEATURE_LEVEL_11_0
                    };

                    fixed (ID3D11Device** device = &_device)
                    {
                        ThrowIfFailed("D3D11CreateDevice", D3D11CreateDevice(
                            (IDXGIAdapter*) _adapter,
                            _adapter == null ? D3D_DRIVER_TYPE_HARDWARE : D3D_DRIVER_TYPE_UNKNOWN,
                            IntPtr.Zero,
                            0,
                            featureLevels,
                            1,
                            D3D11_SDK_VERSION,
                            device,
                            null,
                            null
                        ));
                    }

                    // Get adapter from the device.

                    iid = IID_IDXGIDevice1;
                    ThrowIfFailed("QueryInterface", _device->QueryInterface(&iid, (void**) &dxgiDevice));

                    fixed (IDXGIAdapter1** ptrAdapter = &_adapter)
                    {
                        iid = IID_IDXGIAdapter1;
                        ThrowIfFailed("GetParent", dxgiDevice->GetParent(&iid, (void**) ptrAdapter));
                    }

                    Logger.DebugS("clyde.ogl.angle", "Successfully created D3D11 device!");
                }
                finally
                {
                    if (dxgiDevice != null)
                        dxgiDevice->Release();
                }
            }


            public override void Shutdown()
            {
                // Shut down ANGLE.
                if (_eglDisplay != null)
                    eglTerminate(_eglDisplay);

                if (_eglDevice != null)
                    eglReleaseDeviceANGLE(_eglDevice);

                // Shut down D3D11/DXGI
                if (_factory != null)
                    _factory->Release();

                if (_adapter != null)
                    _adapter->Release();

                if (_device != null)
                    _device->Release();
            }

            public override void SwapAllBuffers()
            {
                foreach (var data in _windowData.Values)
                {
                    data.SwapChain->Present(_swapInterval, 0);
                }
            }

            public override void WindowResized(WindowReg reg, Vector2i oldSize)
            {
                var data = _windowData[reg.Id];
                DestroyBackbuffer(data);

                ThrowIfFailed("ResizeBuffers", data.SwapChain->ResizeBuffers(
                    2,
                    (uint) reg.FramebufferSize.X, (uint) reg.FramebufferSize.Y,
                    DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
                    0));

                SetupBackbuffer(data);

                if (reg.IsMainWindow)
                    eglMakeCurrent(_eglDisplay, data.EglBackbuffer, data.EglBackbuffer, _eglContext);
            }

            private IDXGIAdapter1* TryFindAdapterWithName(string name)
            {
                uint idx = 0;

                while (true)
                {
                    IDXGIAdapter1* adapter;
                    var hr = _factory->EnumAdapters1(idx++, &adapter);
                    if (hr == DXGI_ERROR_NOT_FOUND)
                        break;

                    ThrowIfFailed("EnumAdapters1", hr);

                    DXGI_ADAPTER_DESC1 desc;
                    ThrowIfFailed("GetDesc1", adapter->GetDesc1(&desc));

                    var descName = new ReadOnlySpan<char>(desc.Description, 128);

                    if (descName.StartsWith(name))
                        return adapter;

                    adapter->Release();
                }

                return null;
            }

            public override void* GetProcAddress(string name)
            {
                Span<byte> buf = stackalloc byte[128];
                var len = Encoding.UTF8.GetBytes(name, buf);
                buf[len] = 0;

                fixed (byte* ptr = &buf[0])
                {
                    return eglGetProcAddress(ptr);
                }
            }

            public override void BindWindowRenderTarget(WindowId rtWindowId)
            {
                var data = _windowData[rtWindowId];
                var result = eglMakeCurrent(_eglDisplay, data.EglBackbuffer, data.EglBackbuffer, _eglContext);
                if (result == EGL_FALSE)
                    throw new Exception("eglMakeCurrent failed.");

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Clyde.CheckGlError();
            }

            private static void ThrowIfFailed(string methodName, HRESULT hr)
            {
                if (FAILED(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            private static string DumpEglConfig(void* display, void* config)
            {
                var sb = new StringBuilder();

                sb.Append($"cfg: {Get(EGL_CONFIG_ID):00} | ");
                sb.AppendFormat(
                    "R/G/B/A/D/S: {0}/{1}/{2}/{3}/{4:00}/{5} | ",
                    Get(EGL_RED_SIZE), Get(EGL_GREEN_SIZE), Get(EGL_BLUE_SIZE), Get(EGL_ALPHA_SIZE),
                    Get(EGL_DEPTH_SIZE), Get(EGL_STENCIL_SIZE));

                // COLOR_BUFFER_TYPE
                sb.Append($"CBT: {Get(EGL_COLOR_BUFFER_TYPE)} | ");
                sb.Append($"CC: {Get(EGL_CONFIG_CAVEAT)} | ");
                sb.Append($"CONF: {Get(EGL_CONFORMANT)} | ");
                sb.Append($"NAT: {Get(EGL_NATIVE_VISUAL_ID)} | ");
                sb.Append($"SAMPLES: {Get(EGL_SAMPLES)} | ");
                sb.Append($"SAMPLE_BUFFERS: {Get(EGL_SAMPLE_BUFFERS)} | ");
                sb.Append($"ORIENTATION: {Get(EGL_OPTIMAL_SURFACE_ORIENTATION_ANGLE)}");

                return sb.ToString();

                int Get(int attrib)
                {
                    int value;
                    if (eglGetConfigAttrib(display, config, attrib, &value) == EGL_FALSE)
                        throw new Exception("eglGetConfigAttrib failed!");

                    return value;
                }
            }

            private sealed class WindowData
            {
                public WindowReg Reg = default!;

                public IDXGISwapChain* SwapChain;
                public ID3D11Texture2D* Backbuffer;
                public void* EglBackbuffer;
            }
        }
    }
}
