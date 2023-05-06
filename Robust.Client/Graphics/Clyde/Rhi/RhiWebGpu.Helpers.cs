using System.Runtime.InteropServices;
using Silk.NET.WebGPU;
using RColor = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private static string WgpuVersionToString(uint version)
    {
        var a = (version >> 24) & 0xFF;
        var b = (version >> 16) & 0xFF;
        var c = (version >> 08) & 0xFF;
        var d = (version >> 00) & 0xFF;

        return $"{a}.{b}.{c}.{d}";
    }

    private static Color WgpuColor(RColor color) => new()
    {
        R = color.R,
        G = color.G,
        B = color.B,
        A = color.A
    };

    private static string MarshalFromString(byte* str)
    {
        return Marshal.PtrToStringUTF8((nint)str)!;
    }

}
