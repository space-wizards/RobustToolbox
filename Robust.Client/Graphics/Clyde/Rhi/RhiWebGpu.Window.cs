using System;
using Robust.Shared.Utility;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    public sealed class WindowData
    {
        public Surface* Surface;
        public SwapChain* SwapChain;
    }

    private void CreateSurfaceForWindow(Clyde.WindowReg window)
    {
        DebugTools.Assert(_clyde._windowing != null);

        SurfaceDescriptor surfaceDesc = default;
        SurfaceDescriptorFromWindowsHWND surfaceDescHwnd;
        SurfaceDescriptorFromXlibWindow surfaceDescX11;
        SurfaceDescriptorFromMetalLayer surfaceDescMetal;

        if (OperatingSystem.IsWindows())
        {
            var hInstance = _clyde._windowing.WindowGetWin32Instance(window);
            var hWnd = _clyde._windowing.WindowGetWin32Window(window);

            surfaceDescHwnd = new SurfaceDescriptorFromWindowsHWND
            {
                Chain =
                {
                    SType = SType.SurfaceDescriptorFromWindowsHwnd
                },
                Hinstance = hInstance,
                Hwnd = hWnd
            };

            surfaceDesc.NextInChain = (ChainedStruct*) (&surfaceDescHwnd);
        }
        else
        {
            var xDisplay = _clyde._windowing.WindowGetX11Display(window);
            var xWindow = _clyde._windowing.WindowGetX11Id(window);

            if (xDisplay != null && xWindow != null)
            {
                surfaceDescX11 = new SurfaceDescriptorFromXlibWindow
                {
                    Chain =
                    {
                        SType = SType.SurfaceDescriptorFromXlibWindow
                    },
                    Display = ((IntPtr) xDisplay.Value).ToPointer(),
                    Window = xWindow.Value
                };

                surfaceDesc.NextInChain = (ChainedStruct*) (&surfaceDescX11);
            }
            else
            {
                var metalLayer = _clyde._windowing.WindowGetMetalLayer(window);

                if (metalLayer != null)
                {
                    surfaceDescMetal = new SurfaceDescriptorFromMetalLayer
                    {
                        Chain =
                        {
                            SType = SType.SurfaceDescriptorFromMetalLayer
                        },
                        Layer = ((IntPtr) metalLayer.Value).ToPointer()
                    };

                    surfaceDesc.NextInChain = (ChainedStruct*) (&surfaceDescMetal);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        var surface = _webGpu.InstanceCreateSurface(_wgpuInstance, &surfaceDesc);
        window.RhiWebGpuData = new WindowData
        {
            Surface = surface
        };
    }

    private void CreateSwapChainForWindow(Clyde.WindowReg window)
    {
        // TODO: Safety
        var rhiData = window.RhiWebGpuData!;

        var format = TextureFormat.Bgra8UnormSrgb;
        _sawmill.Debug($"Preferred surface format is {format}");

        var swapChainDesc = new SwapChainDescriptor
        {
            Format = format,
            Height = (uint)window.FramebufferSize.Y,
            Width = (uint)window.FramebufferSize.X,
            Usage = TextureUsage.RenderAttachment,
            PresentMode = PresentMode.Fifo
        };

        var swapChain = _webGpu.DeviceCreateSwapChain(_wgpuDevice, rhiData.Surface, &swapChainDesc);
        rhiData.SwapChain = swapChain;

        _sawmill.Debug("WebGPU Surface created!");
    }

    internal override void WindowCreated(Clyde.WindowReg reg)
    {
        CreateSurfaceForWindow(reg);
        CreateSwapChainForWindow(reg);
    }

    internal override void WindowDestroy(Clyde.WindowReg reg)
    {
        var rhiData = reg.RhiWebGpuData!;

        _wgpu.SwapChainDrop(rhiData.SwapChain);
        _wgpu.SurfaceDrop(rhiData.Surface);

        reg.RhiWebGpuData = null;
    }

    internal override void WindowRecreateSwapchain(Clyde.WindowReg reg)
    {
        var rhiData = reg.RhiWebGpuData!;

        _wgpu.SwapChainDrop(rhiData.SwapChain);

        CreateSwapChainForWindow(reg);
    }

    internal override void WindowPresent(Clyde.WindowReg reg)
    {
        // TODO: Safety
        var rhiData = reg.RhiWebGpuData!;

        _webGpu.SwapChainPresent(rhiData.SwapChain);
    }
}
