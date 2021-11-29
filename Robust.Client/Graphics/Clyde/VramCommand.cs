using System;
using System.Runtime.InteropServices;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.DirectX.DXGI;

namespace Robust.Client.Graphics.Clyde
{
    public sealed class VramCommand : IConsoleCommand
    {
        public string Command => "vram";
        public string Description => "Displays video memory usage statics by the game.";
        public string Help => "Usage: vram";

        public unsafe void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                shell.WriteError("This command is only supported on Windows.");
                return;
            }

            IDXGIFactory1* dxgiFactory;
            ThrowIfFailed(nameof(CreateDXGIFactory1), CreateDXGIFactory1(__uuidof<IDXGIFactory1>(), (void**) &dxgiFactory));

            uint idx = 0;
            IDXGIAdapter* adapter;
            while (dxgiFactory->EnumAdapters(idx, &adapter) != DXGI_ERROR_NOT_FOUND)
            {
                DXGI_ADAPTER_DESC2 desc;
                IDXGIAdapter3* adapter3;
                adapter->QueryInterface(__uuidof<IDXGIAdapter3>(), (void**) &adapter3);
                adapter->Release();
                ThrowIfFailed("GetDesc", adapter3->GetDesc2(&desc));

                var descString = new ReadOnlySpan<char>(desc.Description, 128).TrimEnd('\0');
                shell.WriteLine(descString.ToString());

                DXGI_QUERY_VIDEO_MEMORY_INFO memInfo;

                adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &memInfo);
                shell.WriteLine($"Usage (local): {ByteHelpers.FormatBytes((long) memInfo.CurrentUsage)}");

                adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &memInfo);
                shell.WriteLine($"Usage (non local): {ByteHelpers.FormatBytes((long) memInfo.CurrentUsage)}");

                idx += 1;

                adapter3->Release();
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
