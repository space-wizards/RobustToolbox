using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    // wgpu-native and Dawn can't agree on how resources should be dropped
    // Dawn wants reference counting, wgpu doesn't.
    // This will (in the future) abstract the two.

    private void WgpuDropTextureView(TextureView* tv) => _wgpu.TextureViewDrop(tv);
}
