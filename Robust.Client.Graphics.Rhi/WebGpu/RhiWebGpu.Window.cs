using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private RhiTextureFormat _mainTextureFormat;
    private WGPUPresentMode[] _availPresentModes = [];

    public override RhiTextureFormat MainTextureFormat => _mainTextureFormat;

    public sealed class WindowData
    {
        public WGPUSurface Surface;
    }

    private WindowData CreateSurfaceForWindow(in RhiWindowSurfaceParams surfaceParams)
    {
        WGPUSurfaceDescriptor surfaceDesc = default;

#if WINDOWS
        var surfaceDescHwnd = new WGPUSurfaceSourceWindowsHWND
        {
            chain =
            {
                sType = WGPUSType.WGPUSType_SurfaceSourceWindowsHWND
            },
            hinstance = surfaceParams.HInstance,
            hwnd = surfaceParams.HWnd,
        };

        surfaceDesc.nextInChain = (WGPUChainedStruct*)(&surfaceDescHwnd);

#elif MACOS
        var surfaceDescMetal = new WGPUSurfaceSourceMetalLayer
        {
            chain =
            {
                sType = WGPUSType.WGPUSType_SurfaceSourceMetalLayer
            },
            layer = surfaceParams.MetalLayer
        };

        surfaceDesc.nextInChain = (WGPUChainedStruct*)(&surfaceDescMetal);

#elif LINUX
        // TODO: Linux surface creation
        /*
        WGPUSurfaceSourceXlibWindow surfaceDescX11;
        var xDisplay = _clyde._windowing.WindowGetX11Display(window);
        var xWindow = _clyde._windowing.WindowGetX11Id(window);

        if (xDisplay != null && xWindow != null)
        {
            surfaceDescX11 = new WGPUSurfaceSourceXlibWindow
            {
                chain =
                {
                    sType = WGPUSType.WGPUSType_SurfaceSourceXlibWindow
                },
                display = ((IntPtr)xDisplay.Value).ToPointer(),
                window = xWindow.Value
            };

            surfaceDesc.nextInChain = (WGPUChainedStruct*)(&surfaceDescX11);
        */
#endif

        var surface = wgpuInstanceCreateSurface(_wgpuInstance, &surfaceDesc);
        return new WindowData
        {
            Surface = surface
        };
    }

    private void DecideMainTextureFormat(WindowData mainWindow)
    {
        WGPUSurfaceCapabilities surfaceCaps;
        var res = wgpuSurfaceGetCapabilities(mainWindow.Surface, _wgpuAdapter, &surfaceCaps);
        if (res != WGPUStatus.WGPUStatus_Success)
            throw new RhiException("wgpuSurfaceGetCapabilities failed");

        var modes = new Span<WGPUPresentMode>(surfaceCaps.presentModes, (int)surfaceCaps.presentModeCount);
        _availPresentModes = modes.ToArray();
        _sawmill.Debug($"Available present modes: {string.Join(", ", _availPresentModes)}");

        var formats = new Span<WGPUTextureFormat>(surfaceCaps.formats, (int)surfaceCaps.formatCount);

        var found = false;
        foreach (var format in formats)
        {
            if (format == WGPUTextureFormat.WGPUTextureFormat_BGRA8UnormSrgb ||
                format == WGPUTextureFormat.WGPUTextureFormat_RGBA8UnormSrgb)
            {
                found = true;
                _mainTextureFormat = ToRhiFormat(format);
                break;
            }
        }

        _sawmill.Debug($"Available surface formats: {string.Join(", ", formats.ToArray())}");

        if (!found)
            throw new RhiException("Unable to find suitable surface format for main window!");

        _sawmill.Debug($"Preferred surface format is {_mainTextureFormat}");

        wgpuSurfaceCapabilitiesFreeMembers(surfaceCaps);
    }

    private void ConfigureSurface(WindowData window, Vector2i size, bool vsync)
    {
        var swapChainDesc = new WGPUSurfaceConfiguration
        {
            format = ValidateTextureFormat(_mainTextureFormat),
            width = (uint)size.X,
            height = (uint)size.Y,
            usage = WGPUTextureUsage_RenderAttachment,
            presentMode = WGPUPresentMode.WGPUPresentMode_Fifo,
            device = _wgpuDevice
        };

        if (!vsync)
        {
            if (_availPresentModes.Contains(WGPUPresentMode.WGPUPresentMode_Immediate))
                swapChainDesc.presentMode = WGPUPresentMode.WGPUPresentMode_Immediate;
            else if (_availPresentModes.Contains(WGPUPresentMode.WGPUPresentMode_Mailbox))
                swapChainDesc.presentMode = WGPUPresentMode.WGPUPresentMode_Mailbox;
        }

        wgpuSurfaceConfigure(window.Surface, &swapChainDesc);

        _sawmill.Verbose("WebGPU Surface reconfigured!");
    }

    internal override WindowData WindowCreated(in RhiWindowSurfaceParams surfaceParams, Vector2i size, bool vsync)
    {
        var windowData = CreateSurfaceForWindow(in surfaceParams);
        ConfigureSurface(windowData, size, vsync);
        return windowData;
    }

    internal override void WindowDestroy(WindowData reg)
    {
        wgpuSurfaceUnconfigure(reg.Surface);
        wgpuSurfaceRelease(reg.Surface);
    }

    internal override void WindowRecreateSwapchain(WindowData reg, Vector2i size, bool vsyncEnabled)
    {
        ConfigureSurface(reg, size, vsyncEnabled);
    }

    internal override void WindowPresent(WindowData reg)
    {
        // TODO: Safety
        wgpuSurfacePresent(reg.Surface);
    }
}
