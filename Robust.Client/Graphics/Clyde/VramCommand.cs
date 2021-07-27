using System;
using System.Runtime.InteropServices;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using TerraFX.Interop;
using static TerraFX.Interop.D3D_DRIVER_TYPE;
using static TerraFX.Interop.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.DXGI_SWAP_EFFECT;
using static TerraFX.Interop.Windows;

namespace Robust.Client.Graphics.Clyde
{
    public sealed class VramCommand : IConsoleCommand
    {
        public string Command => "vram";
        public string Description => "Checks vram";
        public string Help => "Usage: vram";

        public unsafe void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IDXGIFactory1* dxgiFactory;
            var iid = IID_IDXGIFactory1;
            ThrowIfFailed(nameof(CreateDXGIFactory1), CreateDXGIFactory1(&iid, (void**) &dxgiFactory));

            uint idx = 0;
            IDXGIAdapter* adapter;
            while (dxgiFactory->EnumAdapters(idx, &adapter) != DXGI_ERROR_NOT_FOUND)
            {
                DXGI_ADAPTER_DESC2 desc;
                IDXGIAdapter3* adapter3;
                iid = IID_IDXGIAdapter3;
                adapter->QueryInterface(&iid, (void**) &adapter3);
                ThrowIfFailed("GetDesc", adapter3->GetDesc2(&desc));

                var descString = new ReadOnlySpan<char>(desc.Description, 128).TrimEnd('\0');
                shell.WriteLine(descString.ToString());

                DXGI_QUERY_VIDEO_MEMORY_INFO memInfo;

                adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &memInfo);
                shell.WriteLine($"Usage (local): {ByteHelpers.FormatBytes((long) memInfo.CurrentUsage)}");

                adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &memInfo);
                shell.WriteLine($"Usage (non local): {ByteHelpers.FormatBytes((long) memInfo.CurrentUsage)}");

                idx += 1;
            }
        }

        private static void ThrowIfFailed(string methodName, HRESULT hr)
        {
            if (FAILED(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
