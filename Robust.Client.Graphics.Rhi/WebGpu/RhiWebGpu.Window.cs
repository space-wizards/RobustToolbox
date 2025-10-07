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

#elif OSX
        // TODO: macOS surface creation
        WGPUSurfaceSourceMetalLayer surfaceDescMetal;
        /*
        var metalLayer = _clyde._windowing.WindowGetMetalLayer(window);

        if (metalLayer != null)
        {
            surfaceDescMetal = new WGPUSurfaceSourceMetalLayer
            {
                chain =
                {
                    sType = WGPUSType.WGPUSType_SurfaceSourceMetalLayer
                },
                layer = ((IntPtr)metalLayer.Value).ToPointer()
            };

            surfaceDesc.nextInChain = (WGPUChainedStruct*)(&surfaceDescMetal);
        }
        else
        {
            throw new NotImplementedException();
        }
        */
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
