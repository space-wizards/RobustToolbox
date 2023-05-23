using System;
using System.Collections.Generic;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, CommandEncoderReg> _commandEncoderRegistry = new();
    private readonly Dictionary<RhiHandle, RenderPassEncoderReg> _renderPassEncoderRegistry = new();

    public override RhiCommandEncoder CreateCommandEncoder(in RhiCommandEncoderDescriptor descriptor)
    {
        CommandEncoder* nativeEncoder;
        fixed (byte* pLabel = MakeLabel(descriptor.Label))
        {
            var nativeDescriptor = new CommandEncoderDescriptor
            {
                Label = pLabel
            };

            nativeEncoder = _webGpu.DeviceCreateCommandEncoder(_wgpuDevice, &nativeDescriptor);
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

        var pDescriptor = BumpAllocate<RenderPassDescriptor>(ref buffer);
        pDescriptor->Label = BumpAllocateUtf8(ref buffer, descriptor.Label);

        var colorAttachments = descriptor.ColorAttachments;
        pDescriptor->ColorAttachmentCount = (uint)colorAttachments.Length;
        pDescriptor->ColorAttachments = BumpAllocate<RenderPassColorAttachment>(ref buffer, colorAttachments.Length);
        for (var i = 0; i < colorAttachments.Length; i++)
        {
            ref var attachment = ref colorAttachments[i];
            var pAttachment = &pDescriptor->ColorAttachments[i];
            pAttachment->View = _textureViewRegistry[attachment.View.Handle].Native;
            if (attachment.ResolveTarget is { } resolveTarget)
                pAttachment->ResolveTarget = _textureViewRegistry[resolveTarget.Handle].Native;
            pAttachment->ClearValue = WgpuColor(attachment.ClearValue);
            pAttachment->LoadOp = (LoadOp)attachment.LoadOp;
            pAttachment->StoreOp = (StoreOp)attachment.StoreOp;
        }

        if (descriptor.DepthStencilAttachment is { } depthStencilAttachment)
        {
            var pDepthStencilAttachment = BumpAllocate<RenderPassDepthStencilAttachment>(ref buffer);
            pDescriptor->DepthStencilAttachment = pDepthStencilAttachment;

            pDepthStencilAttachment->View = _textureViewRegistry[depthStencilAttachment.View.Handle].Native;
            pDepthStencilAttachment->DepthLoadOp = (LoadOp)depthStencilAttachment.DepthLoadOp;
            pDepthStencilAttachment->DepthStoreOp = (StoreOp)depthStencilAttachment.DepthStoreOp;
            pDepthStencilAttachment->DepthClearValue = depthStencilAttachment.DepthClearValue;
            pDepthStencilAttachment->DepthReadOnly = depthStencilAttachment.DepthReadOnly;
            pDepthStencilAttachment->StencilLoadOp = (LoadOp)depthStencilAttachment.StencilLoadOp;
            pDepthStencilAttachment->StencilStoreOp = (StoreOp)depthStencilAttachment.StencilStoreOp;
            pDepthStencilAttachment->StencilClearValue = depthStencilAttachment.StencilClearValue;
            pDepthStencilAttachment->StencilReadOnly = depthStencilAttachment.StencilReadOnly;
        }

        if (descriptor.OcclusionQuerySet != null)
            throw new NotImplementedException();

        var pDescriptorMaxDrawCount = BumpAllocate<RenderPassDescriptorMaxDrawCount>(ref buffer);
        pDescriptor->NextInChain = (ChainedStruct*)pDescriptorMaxDrawCount;
        pDescriptorMaxDrawCount->Chain.SType = SType.RenderPassDescriptorMaxDrawCount;
        pDescriptorMaxDrawCount->MaxDrawCount = descriptor.MaxDrawCount;

        var nativeEncoder = _webGpu.CommandEncoderBeginRenderPass(
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
        _webGpu.RenderPassEncoderSetPipeline(
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
        _webGpu.RenderPassEncoderDraw(
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

        _webGpu.RenderPassEncoderEnd(_renderPassEncoderRegistry[handle].Native);
        RenderPassEncoderDropped(handle);
    }

    internal override void RenderPassEncoderSetBindGroup(
        RhiRenderPassEncoder encoder,
        uint index,
        RhiBindGroup? bindGroup)
    {
        _webGpu.RenderPassEncoderSetBindGroup(
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
        Buffer* nativeBuffer = null;
        if (buffer != null)
            nativeBuffer = _bufferRegistry[buffer.Handle].Native;

        _webGpu.RenderPassEncoderSetVertexBuffer(
            _renderPassEncoderRegistry[encoder.Handle].Native,
            slot,
            nativeBuffer,
            offset,
            size ?? WebGPU.WholeSize
        );
    }

    internal override void RenderPassEncoderSetScissorRect(
        RhiRenderPassEncoder encoder,
        uint x, uint y, uint w, uint h)
    {
        // TODO: safety
        _webGpu.RenderPassEncoderSetScissorRect(
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
        var pDescriptor = BumpAllocate<CommandBufferDescriptor>(ref buffer);
        pDescriptor->Label = BumpAllocateUtf8(ref buffer, descriptor.Label);

        var nativeBuffer = _webGpu.CommandEncoderFinish(
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
        public CommandEncoder* Native;
    }

    private sealed class RenderPassEncoderReg
    {
        public RenderPassEncoder* Native;
    }

    private sealed class CommandBufferReg
    {
        public CommandBuffer* Native;
    }
}
