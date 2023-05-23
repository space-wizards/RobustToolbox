using System;
using System.Threading.Tasks;

namespace Robust.Client.Graphics.Clyde.Rhi;

public abstract partial class RhiBase
{
    //
    // Clyde <-> RHI API.
    //

    internal abstract void Init();
    internal abstract void Shutdown();

    /// <summary>
    /// A window was created by Clyde. It should be initialized by the RHI to make it ready for rendering.
    /// </summary>
    /// <remarks>
    /// Does not get called for the main window.
    /// </remarks>
    internal abstract void WindowCreated(Clyde.WindowReg reg);

    /// <summary>
    /// A window is about to be destroyed by Clyde. Clean up resources for it.
    /// </summary>
    internal abstract void WindowDestroy(Clyde.WindowReg reg);

    /// <summary>
    /// Recreate the native swap chain, in case it has become suboptimal (e.g. due to window resizing).
    /// </summary>
    internal abstract void WindowRecreateSwapchain(Clyde.WindowReg reg);

    internal abstract RhiTextureView CreateTextureViewForWindow(Clyde.WindowReg reg);
    internal abstract void WindowPresent(Clyde.WindowReg reg);

    //
    // RHI-internal API to de-OOP the public RHI API.
    //

    internal abstract RhiRenderPassEncoder CommandEncoderBeginRenderPass(
        RhiCommandEncoder encoder,
        in RhiRenderPassDescriptor descriptor
    );

    internal abstract RhiCommandBuffer CommandEncoderFinish(in RhiCommandEncoder encoder,
        in RhiCommandBufferDescriptor descriptor);

    internal abstract void RenderPassEncoderSetPipeline(
        in RhiRenderPassEncoder encoder,
        RhiRenderPipeline pipeline
    );

    internal abstract void RenderPassEncoderDraw(
        in RhiRenderPassEncoder encoder,
        uint vertexCount,
        uint instanceCount,
        uint firstVertex,
        uint firstInstance
    );

    internal abstract void RenderPassEncoderEnd(RhiRenderPassEncoder encoder);

    internal abstract void QueueSubmit(RhiQueue queue, RhiCommandBuffer[] commandBuffers);

    internal abstract void QueueWriteTexture(
        RhiQueue queue,
        in RhiImageCopyTexture destination,
        ReadOnlySpan<byte> data,
        in RhiImageDataLayout dataLayout,
        RhiExtent3D size
    );

    public abstract void QueueWriteBuffer(RhiBuffer buffer, ulong bufferOffset, ReadOnlySpan<byte> data);

    internal abstract RhiTextureView TextureCreateView(RhiTexture texture, in RhiTextureViewDescriptor descriptor);
    internal abstract void TextureViewDrop(RhiTextureView textureView);
    internal abstract void BindGroupDrop(RhiBindGroup rhiBindGroup);

    internal abstract void RenderPassEncoderSetBindGroup(
        RhiRenderPassEncoder encoder,
        uint index,
        RhiBindGroup? bindGroup
    );

    internal abstract void RenderPassEncoderSetVertexBuffer(RhiRenderPassEncoder encoder,
        uint slot,
        RhiBuffer? buffer,
        ulong offset,
        ulong? size);

    internal abstract void RenderPassEncoderSetScissorRect(RhiRenderPassEncoder encoder,
        uint x,
        uint y,
        uint w,
        uint h);

    internal abstract void CommandBufferDrop(RhiCommandBuffer commandBuffer);

    internal abstract RhiBufferMapState BufferGetMapState(RhiBuffer buffer);
    internal abstract ValueTask BufferMapAsync(RhiBuffer buffer, RhiMapModeFlags mode, nuint offset, nuint size);
    internal abstract RhiMappedBufferRange BufferGetMappedRange(RhiBuffer buffer, nuint offset, nuint size);
    internal abstract void BufferUnmap(RhiBuffer buffer);
    internal abstract void BufferDrop(RhiBuffer buffer);
}

internal record struct RhiHandle(long Value);
