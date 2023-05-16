using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, RenderPipelineReg> _renderPipelineRegistry = new();
    private readonly Dictionary<RhiHandle, PipelineLayoutReg> _pipelineLayoutRegistry = new();

    public override RhiPipelineLayout CreatePipelineLayout(in RhiPipelineLayoutDescriptor descriptor)
    {
        // TODO: SAFETY

        Span<byte> buffer = stackalloc byte[128];
        var pDescriptor = BumpAllocate<PipelineLayoutDescriptor>(ref buffer);
        pDescriptor->Label = BumpAllocateUtf8(ref buffer, descriptor.Label);

        var layouts = descriptor.BindGroupLayouts;
        pDescriptor->BindGroupLayoutCount = (uint) layouts.Length;
        pDescriptor->BindGroupLayouts = BumpAllocatePtr<BindGroupLayout>(ref buffer, layouts.Length);
        for (var i = 0; i < layouts.Length; i++)
        {
            pDescriptor->BindGroupLayouts[i] = _bindGroupLayoutRegistry[layouts[i].Handle].Native;
        }

        var native = _webGpu.DeviceCreatePipelineLayout(_wgpuDevice, pDescriptor);

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _pipelineLayoutRegistry.Add(handle, new PipelineLayoutReg { Native = native });
        return new RhiPipelineLayout(this, handle);
    }

    public override RhiRenderPipeline CreateRenderPipeline(in RhiRenderPipelineDescriptor descriptor)
    {
        // TODO: THREAD SAFETY
        // TODO: INPUT VALIDATION

        var vertexShader = _shaderModuleRegistry[descriptor.Vertex.ProgrammableStage.ShaderModule.Handle].Native;

        const int bufferSize = 8192;
        var bufferPtr = NativeMemory.AlignedAlloc(bufferSize, 8);

        RenderPipeline* nativePipeline;
        try
        {
            var buffer = new Span<byte>(bufferPtr, bufferSize);

            RenderPipelineDescriptor pipelineDesc = default;
            pipelineDesc.Label = BumpAllocateUtf8(ref buffer, descriptor.Label);

            // Pipeline layout
            switch (descriptor.Layout)
            {
                case RhiPipelineLayout pipelineLayout:
                    pipelineDesc.Layout = _pipelineLayoutRegistry[pipelineLayout.Handle].Native;
                    break;

                case RhiAutoLayoutMode:
                    throw new NotSupportedException("wgpu does not support auto layout yet");
                // Default case: no layout given, do nothing
            }

            // Vertex state
            pipelineDesc.Vertex.Module = vertexShader;
            pipelineDesc.Vertex.EntryPoint = BumpAllocateUtf8(
                ref buffer,
                descriptor.Vertex.ProgrammableStage.EntryPoint);

            WgpuProgrammableConstants(
                ref buffer,
                descriptor.Vertex.ProgrammableStage.Constants,
                out pipelineDesc.Vertex.ConstantCount,
                out pipelineDesc.Vertex.Constants);

            var buffers = descriptor.Vertex.Buffers;
            pipelineDesc.Vertex.BufferCount = (uint)buffers.Length;
            pipelineDesc.Vertex.Buffers = BumpAllocate<VertexBufferLayout>(ref buffer, buffers.Length);
            for (var i = 0; i < buffers.Length; i++)
            {
                ref var bufferLayout = ref pipelineDesc.Vertex.Buffers[i];
                bufferLayout.ArrayStride = buffers[i].ArrayStride;
                bufferLayout.StepMode = (VertexStepMode)buffers[i].StepMode;

                var attributes = buffers[i].Attributes;
                bufferLayout.AttributeCount = (uint)attributes.Length;
                bufferLayout.Attributes = BumpAllocate<VertexAttribute>(ref buffer, attributes.Length);
                for (var j = 0; j < attributes.Length; j++)
                {
                    ref var attribute = ref bufferLayout.Attributes[j];
                    attribute.Format = (VertexFormat)attributes[j].Format;
                    attribute.Offset = attributes[j].Offset;
                    attribute.ShaderLocation = attributes[j].ShaderLocation;
                }
            }

            // Primitive state
            pipelineDesc.Primitive.Topology = (PrimitiveTopology)descriptor.Primitive.Topology;
            pipelineDesc.Primitive.StripIndexFormat = (IndexFormat)descriptor.Primitive.StripIndexformat;
            pipelineDesc.Primitive.FrontFace = (FrontFace)descriptor.Primitive.FrontFace;
            pipelineDesc.Primitive.CullMode = (CullMode)descriptor.Primitive.CullMode;

            var pPrimitiveDepthClipControl = BumpAllocate<PrimitiveDepthClipControl>(ref buffer);
            pPrimitiveDepthClipControl->Chain.SType = SType.PrimitiveDepthClipControl;
            pipelineDesc.Primitive.NextInChain = &pPrimitiveDepthClipControl->Chain;

            pPrimitiveDepthClipControl->UnclippedDepth = descriptor.Primitive.UnclippedDepth;

            // Depth stencil state
            if (descriptor.DepthStencil is { } depthStencil)
            {
                var pDepthStencil = BumpAllocate<DepthStencilState>(ref buffer);
                pipelineDesc.DepthStencil = pDepthStencil;

                pDepthStencil->Format = (TextureFormat)depthStencil.Format;
                pDepthStencil->DepthWriteEnabled = depthStencil.DepthWriteEnabled;
                pDepthStencil->DepthCompare = (CompareFunction)depthStencil.DepthCompare;
                pDepthStencil->StencilFront = WgpuStencilFaceState(depthStencil.StencilFront ?? new RhiStencilFaceState());
                pDepthStencil->StencilBack = WgpuStencilFaceState(depthStencil.StencilBack ?? new RhiStencilFaceState());
                pDepthStencil->StencilReadMask = depthStencil.StencilReadMask;
                pDepthStencil->StencilWriteMask = depthStencil.StencilWriteMask;
                pDepthStencil->DepthBias = depthStencil.DepthBias;
                pDepthStencil->DepthBiasSlopeScale = depthStencil.DepthBiasSlopeScale;
                pDepthStencil->DepthBiasClamp = depthStencil.DepthBiasClamp;
            }

            // Multisample state
            pipelineDesc.Multisample.Count = descriptor.Multisample.Count;
            pipelineDesc.Multisample.Mask = descriptor.Multisample.Mask;
            pipelineDesc.Multisample.AlphaToCoverageEnabled = descriptor.Multisample.AlphaToCoverageEnabled;

            // Fragment state
            if (descriptor.Fragment is { } fragment)
            {
                var fragmentShader = _shaderModuleRegistry[fragment.ProgrammableStage.ShaderModule.Handle].Native;

                var pFragment = BumpAllocate<FragmentState>(ref buffer);
                pipelineDesc.Fragment = pFragment;

                pFragment->Module = fragmentShader;
                pFragment->EntryPoint = BumpAllocateUtf8(ref buffer, fragment.ProgrammableStage.EntryPoint);

                WgpuProgrammableConstants(
                    ref buffer,
                    fragment.ProgrammableStage.Constants,
                    out pFragment->ConstantCount,
                    out pFragment->Constants);

                var targets = fragment.Targets;
                pFragment->TargetCount = (uint)targets.Length;
                pFragment->Targets = BumpAllocate<ColorTargetState>(ref buffer, targets.Length);
                for (var i = 0; i < targets.Length; i++)
                {
                    ref var target = ref pFragment->Targets[i];
                    target.Format = (TextureFormat)targets[i].Format;

                    if (targets[i].Blend is { } blend)
                    {
                        var pBlend = BumpAllocate<BlendState>(ref buffer);
                        target.Blend = pBlend;

                        pBlend->Alpha = WgpuBlendComponent(blend.Alpha);
                        pBlend->Color = WgpuBlendComponent(blend.Color);
                    }

                    target.WriteMask = (ColorWriteMask)targets[i].WriteMask;
                }
            }

            nativePipeline = _webGpu.DeviceCreateRenderPipeline(_wgpuDevice, &pipelineDesc);
        }
        finally
        {
            NativeMemory.AlignedFree(bufferPtr);
        }

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _renderPipelineRegistry.Add(handle, new RenderPipelineReg { Native = nativePipeline });
        return new RhiRenderPipeline(this, handle);
    }

    private static StencilFaceState WgpuStencilFaceState(in RhiStencilFaceState state)
    {
        return new StencilFaceState
        {
            Compare = (CompareFunction)state.Compare,
            FailOp = (StencilOperation)state.FailOp,
            DepthFailOp = (StencilOperation)state.DepthFailOp,
            PassOp = (StencilOperation)state.PassOp
        };
    }

    private static void WgpuProgrammableConstants(
        ref Span<byte> buffer,
        RhiConstantEntry[] constants,
        out uint constantCount,
        out ConstantEntry* pConstants)
    {
        constantCount = (uint)constants.Length;
        pConstants = BumpAllocate<ConstantEntry>(ref buffer, constants.Length);
        for (var i = 0; i < constants.Length; i++)
        {
            ref var constant = ref pConstants[i];
            constant.Key = BumpAllocateUtf8(ref buffer, constants[i].Key);
            constant.Value = constants[i].Value;
        }
    }

    private static BlendComponent WgpuBlendComponent(in RhiBlendComponent component)
    {
        return new BlendComponent
        {
            Operation = (BlendOperation)component.Operation,
            DstFactor = (BlendFactor)component.DstFactor,
            SrcFactor = (BlendFactor)component.SrcFactor,
        };
    }

    private sealed class RenderPipelineReg
    {
        public RenderPipeline* Native;
    }

    private sealed class PipelineLayoutReg
    {
        public PipelineLayout* Native;
    }
}
