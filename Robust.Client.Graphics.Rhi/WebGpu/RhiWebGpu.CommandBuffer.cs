namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, CommandBufferReg> _commandBufferRegistry = new();

    /// <summary>
    /// Command buffer was dropped natively, either via explicit call or implicit side effect (e.g. queue submit).
    /// </summary>
    private void CommandBufferDropped(RhiCommandBuffer commandBuffer)
    {
        _commandBufferRegistry.Remove(commandBuffer.Handle);
        GC.SuppressFinalize(commandBuffer);
    }

    internal override void CommandBufferDrop(RhiCommandBuffer commandBuffer)
    {
        wgpuCommandBufferRelease(_commandBufferRegistry[commandBuffer.Handle].Native);
        CommandBufferDropped(commandBuffer);
    }
}
