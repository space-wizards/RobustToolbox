namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, CommandEncoderReg> _commandEncoderRegistry = new();
    private readonly Dictionary<RhiHandle, RenderPassEncoderReg> _renderPassEncoderRegistry = new();

    public override RhiCommandEncoder CreateCommandEncoder(in RhiCommandEncoderDescriptor descriptor)
    {
        WGPUCommandEncoder nativeEncoder;
        fixed (byte* pLabel = MakeLabel(descriptor.Label))
        {
            var nativeDescriptor = new WGPUCommandEncoderDescriptor
            {
                label = new WGPUStringView {data = (sbyte*)pLabel, length = WGPU_STRLEN}
            };

            nativeEncoder = wgpuDeviceCreateCommandEncoder(_wgpuDevice, &nativeDescriptor);
        }

        // TODO: thread safety
        var handle = AllocRhiHandle();
        _commandEncoderRegistry.Add(handle, new CommandEncoderReg { Native = nativeEncoder });
        return new RhiCommandEncoder(this, handle);
    }

    internal override RhiRenderPassEncoder CommandEncoderBeginRenderPass(
        RhiCommandEncoder encoder,
        in RhiRenderPassDescriptor descriptor)
    {
        // TODO: Ensure not disposed
        // TODO: Thread safety

        Span<byte> buffer = stackalloc byte[512];

        var pDescriptor = BumpAllocate<WGPURenderPassDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);

        var colorAttachments = descriptor.ColorAttachments;
        pDescriptor->colorAttachmentCount = (uint)colorAttachments.Length;
        pDescriptor->colorAttachments = BumpAllocate<WGPURenderPassColorAttachment>(ref buffer, colorAttachments.Length);
        for (var i = 0; i < colorAttachments.Length; i++)
        {
            ref var attachment = ref colorAttachments[i];
            var pAttachment = &pDescriptor->colorAttachments[i];
            pAttachment->view = _textureViewRegistry[attachment.View.Handle].Native;
            if (attachment.ResolveTarget is { } resolveTarget)
                pAttachment->resolveTarget = _textureViewRegistry[resolveTarget.Handle].Native;
            pAttachment->clearValue = WgpuColor(attachment.ClearValue);
            pAttachment->loadOp = (WGPULoadOp)attachment.LoadOp;
            pAttachment->storeOp = (WGPUStoreOp)attachment.StoreOp;
        }

        if (descriptor.DepthStencilAttachment is { } depthStencilAttachment)
        {
            var pDepthStencilAttachment = BumpAllocate<WGPURenderPassDepthStencilAttachment>(ref buffer);
            pDescriptor->depthStencilAttachment = pDepthStencilAttachment;

            pDepthStencilAttachment->view = _textureViewRegistry[depthStencilAttachment.View.Handle].Native;
            pDepthStencilAttachment->depthLoadOp = (WGPULoadOp)depthStencilAttachment.DepthLoadOp;
            pDepthStencilAttachment->depthStoreOp = (WGPUStoreOp)depthStencilAttachment.DepthStoreOp;
            pDepthStencilAttachment->depthClearValue = depthStencilAttachment.DepthClearValue;
            pDepthStencilAttachment->depthReadOnly = depthStencilAttachment.DepthReadOnly ? 1u : 0u;
            pDepthStencilAttachment->stencilLoadOp = (WGPULoadOp)depthStencilAttachment.StencilLoadOp;
            pDepthStencilAttachment->stencilStoreOp = (WGPUStoreOp)depthStencilAttachment.StencilStoreOp;
            pDepthStencilAttachment->stencilClearValue = depthStencilAttachment.StencilClearValue;
            pDepthStencilAttachment->stencilReadOnly = depthStencilAttachment.StencilReadOnly ? 1u : 0u;
        }

        if (descriptor.OcclusionQuerySet != null)
            throw new NotImplementedException();

        var pDescriptorMaxDrawCount = BumpAllocate<WGPURenderPassMaxDrawCount>(ref buffer);
        pDescriptor->nextInChain = (WGPUChainedStruct*)pDescriptorMaxDrawCount;
        pDescriptorMaxDrawCount->chain.sType = WGPUSType.WGPUSType_RenderPassMaxDrawCount;
        pDescriptorMaxDrawCount->maxDrawCount = descriptor.MaxDrawCount;

        var nativeEncoder = wgpuCommandEncoderBeginRenderPass(
            _commandEncoderRegistry[encoder.Handle].Native,
            pDescriptor
        );

        // TODO: thread safety
        var handle = AllocRhiHandle();
        _renderPassEncoderRegistry.Add(handle, new RenderPassEncoderReg { Native = nativeEncoder });
        return new RhiRenderPassEncoder(this, handle);
    }

    internal override void RenderPassEncoderSetPipeline(
        in RhiRenderPassEncoder encoder,
        RhiRenderPipeline pipeline)
    {
        // TODO: safety
        wgpuRenderPassEncoderSetPipeline(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            _renderPipelineRegistry[pipeline.Handle].Native
        );
    }

    internal override void RenderPassEncoderDraw(
        in RhiRenderPassEncoder encoder,
        uint vertexCount,
        uint instanceCount,
        uint firstVertex,
        uint firstInstance)
    {
        // TODO: safety
        wgpuRenderPassEncoderDraw(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            vertexCount,
            instanceCount,
            firstVertex,
            firstInstance
        );
    }

    internal override void RenderPassEncoderEnd(RhiRenderPassEncoder encoder)
    {
        // TODO: safety
        var handle = encoder.Handle;

        wgpuRenderPassEncoderEnd(_renderPassEncoderRegistry[handle].Native);
        RenderPassEncoderDropped(handle);
    }

    internal override void RenderPassEncoderSetBindGroup(
        RhiRenderPassEncoder encoder,
        uint index,
        RhiBindGroup? bindGroup)
    {
        wgpuRenderPassEncoderSetBindGroup(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            index,
            _bindGroupRegistry[bindGroup!.Handle].Native,
            0, null
        );
    }

    internal override void RenderPassEncoderSetVertexBuffer(
        RhiRenderPassEncoder encoder,
        uint slot,
        RhiBuffer? buffer,
        ulong offset,
        ulong? size)
    {
        WGPUBuffer nativeBuffer = null;
        if (buffer != null)
            nativeBuffer = _bufferRegistry[buffer.Handle].Native;

        wgpuRenderPassEncoderSetVertexBuffer(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            slot,
            nativeBuffer,
            offset,
            size ?? WGPU_WHOLE_SIZE
        );
    }

    internal override void RenderPassEncoderSetScissorRect(
        RhiRenderPassEncoder encoder,
        uint x, uint y, uint w, uint h)
    {
        // TODO: safety
        wgpuRenderPassEncoderSetScissorRect(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            x,
            y,
            w,
            h
        );
    }

    internal override RhiCommandBuffer CommandEncoderFinish(
        in RhiCommandEncoder encoder,
        in RhiCommandBufferDescriptor descriptor)
    {
        // TODO: safety
        var handle = encoder.Handle;

        Span<byte> buffer = stackalloc byte[512];
        var pDescriptor = BumpAllocate<WGPUCommandBufferDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);

        var nativeBuffer = wgpuCommandEncoderFinish(
            _commandEncoderRegistry[handle].Native,
            pDescriptor
        );

        CommandEncoderDropped(handle);

        var bufferHandle = AllocRhiHandle();
        _commandBufferRegistry.Add(bufferHandle, new CommandBufferReg { Native = nativeBuffer });
        return new RhiCommandBuffer(this, bufferHandle);
    }

    private void CommandEncoderDropped(RhiHandle encoder)
    {
        _commandEncoderRegistry.Remove(encoder);
    }

    private void RenderPassEncoderDropped(RhiHandle encoder)
    {
        _renderPassEncoderRegistry.Remove(encoder);
    }

    private sealed class CommandEncoderReg
    {
        public WGPUCommandEncoder Native;
    }

    private sealed class RenderPassEncoderReg
    {
        public WGPURenderPassEncoder Native;
    }

    private sealed class CommandBufferReg
    {
        public WGPUCommandBuffer Native;
    }
}
