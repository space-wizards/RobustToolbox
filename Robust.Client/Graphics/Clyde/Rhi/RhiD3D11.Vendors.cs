using System.Collections.Generic;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed partial class RhiD3D11
{
    // https://pci-ids.ucw.cz/read/PC
    private static readonly Dictionary<uint, string> KnownPciDeviceVendors = new()
    {
        { 0x1002, "Advanced Micro Devices, Inc. [AMD/ATI]" },
        { 0x1022, "Advanced Micro Devices, Inc. [AMD]" },
        { 0x10de, "NVIDIA Corporation" },
        { 0x1414, "Microsoft Corporation" },
        { 0x8086, "Intel Corporation" },
    };
}
