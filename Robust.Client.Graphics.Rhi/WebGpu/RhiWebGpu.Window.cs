using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
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
        var surfaceDescWayland = new WGPUSurfaceSourceWaylandSurface
        {
            chain =
            {
                sType = WGPUSType.WGPUSType_SurfaceSourceWaylandSurface
            },
            display = surfaceParams.WaylandDisplay,
            surface = surfaceParams.WaylandSurface,
        };

        var surfaceDescX11 = new WGPUSurfaceSourceXlibWindow()
        {
            chain =
            {
                sType = WGPUSType.WGPUSType_SurfaceSourceXlibWindow
            },
            display = surfaceParams.X11Display,
            // TODO "Oh my god x11 support might be a nightmare this is outside of your ability to deal with -pjb"
            // window = surfaceParams.X11Window,
        };

        if (surfaceParams.Wayland)
        {
            surfaceDesc.nextInChain = (WGPUChainedStruct*)(&surfaceDescWayland);
        }
        else
        {
            surfaceDesc.nextInChain =  (WGPUChainedStruct*)(&surfaceDescX11);
        }
#endif

        var surface = wgpuInstanceCreateSurface(_wgpuInstance, &surfaceDesc);
        return new WindowData
        {
            Surface = surface
        };
    }

    private void ConfigureSurface(WindowData window, Vector2i size)
    {
        // TODO: Safety
        var format = WGPUTextureFormat.WGPUTextureFormat_BGRA8UnormSrgb;
        _sawmill.Debug($"Preferred surface format is {format}");

        var swapChainDesc = new WGPUSurfaceConfiguration
        {
            format = format,
            width = (uint)size.X,
            height = (uint)size.Y,
            usage = WGPUTextureUsage_RenderAttachment,
            presentMode = WGPUPresentMode.WGPUPresentMode_Fifo,
            device = _wgpuDevice
        };

        wgpuSurfaceConfigure(window.Surface, &swapChainDesc);

        _sawmill.Debug("WebGPU Surface created!");
    }

    internal override WindowData WindowCreated(in RhiWindowSurfaceParams surfaceParams, Vector2i size)
    {
        var windowData = CreateSurfaceForWindow(in surfaceParams);
        ConfigureSurface(windowData, size);
        return windowData;
    }

    internal override void WindowDestroy(WindowData reg)
    {
        wgpuSurfaceUnconfigure(reg.Surface);
        wgpuSurfaceRelease(reg.Surface);
    }

    internal override void WindowRecreateSwapchain(WindowData reg, Vector2i size)
    {
        ConfigureSurface(reg, size);
    }

    internal override void WindowPresent(WindowData reg)
    {
        // TODO: Safety
        wgpuSurfacePresent(reg.Surface);
    }
}
