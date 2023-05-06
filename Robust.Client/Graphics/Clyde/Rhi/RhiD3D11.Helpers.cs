using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed partial class RhiD3D11
{
    private static void ThrowIfFailed(string methodName, HRESULT hr)
    {
        if (FAILED(hr))
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
