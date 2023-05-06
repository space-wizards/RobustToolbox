using System;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.DXGI_SWAP_EFFECT;
using static TerraFX.Interop.DirectX.DXGI;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiD3D11
{
    public sealed class WindowData
    {
        public IDXGISwapChain1* SwapChain;
    }

    private void CreateSwapChainForWindow(Clyde.WindowReg reg)
    {
        DXGI_SWAP_CHAIN_DESC1 desc;
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
        {
            // Flip mode
            desc = new DXGI_SWAP_CHAIN_DESC1
            {
                Width = (uint)reg.FramebufferSize.X,
                Height = (uint)reg.FramebufferSize.Y,
                Format = DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc =
                {
                    Count = 1
                },
                BufferCount = 2,
                BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT | DXGI_USAGE_SHADER_INPUT,
            };

            desc.SwapEffect = OperatingSystem.IsWindowsVersionAtLeast(10)
                ? DXGI_SWAP_EFFECT_FLIP_DISCARD
                : DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
        }
        else
        {
            throw new NotImplementedException("Non-flip Windows 7 swap chains not implemented yet");
        }

        var hWnd = _clyde._windowing!.WindowGetWin32Window(reg);

        IDXGISwapChain1* swapChain;
        ThrowIfFailed("CreateSwapChain", _dxgiFactory->CreateSwapChainForHwnd(
            (IUnknown*)_device,
            hWnd,
            &desc,
            null,
            null,
            &swapChain
        ));

        reg.RhiD3D11Data = new WindowData
        {
            SwapChain = swapChain
        };
    }
}
