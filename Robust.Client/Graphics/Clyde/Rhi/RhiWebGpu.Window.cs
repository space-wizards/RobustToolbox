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
            throw new NotImplementedException();
        }

        var surface = _webGpu.InstanceCreateSurface(_wgpuInstance, &surfaceDesc);
        window.RhiWebGpuData = new WindowData
        {
            Surface = surface
        };
    }

    private void CreateSwapChainForWindow(Clyde.WindowReg window)
    {
        var rhiData = window.RhiWebGpuData!;

        var format = _webGpu.SurfaceGetPreferredFormat(rhiData.Surface, _wgpuAdapter);
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

}
