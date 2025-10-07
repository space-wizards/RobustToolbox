using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, RenderPipelineReg> _renderPipelineRegistry = new();
    private readonly Dictionary<RhiHandle, PipelineLayoutReg> _pipelineLayoutRegistry = new();

    public override RhiPipelineLayout CreatePipelineLayout(in RhiPipelineLayoutDescriptor descriptor)
    {
        // TODO: SAFETY

        Span<byte> buffer = stackalloc byte[128];
        var pDescriptor = BumpAllocate<WGPUPipelineLayoutDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);

        var layouts = descriptor.BindGroupLayouts;
        pDescriptor->bindGroupLayoutCount = (uint) layouts.Length;
        pDescriptor->bindGroupLayouts = BumpAllocatePtr<WGPUBindGroupLayoutImpl>(ref buffer, layouts.Length);
        for (var i = 0; i < layouts.Length; i++)
        {
            pDescriptor->bindGroupLayouts[i] = _bindGroupLayoutRegistry[layouts[i].Handle].Native;
        }

        var native = wgpuDeviceCreatePipelineLayout(_wgpuDevice, pDescriptor);

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

        WGPURenderPipeline nativePipeline;
        try
        {
            var buffer = new Span<byte>(bufferPtr, bufferSize);

            WGPURenderPipelineDescriptor pipelineDesc = default;
            pipelineDesc.label = BumpAllocateStringView(ref buffer, descriptor.Label);

            // Pipeline layout
            switch (descriptor.Layout)
            {
                case RhiPipelineLayout pipelineLayout:
                    pipelineDesc.layout = _pipelineLayoutRegistry[pipelineLayout.Handle].Native;
                    break;

                case RhiAutoLayoutMode:
                    throw new NotSupportedException("wgpu does not support auto layout yet");
                // Default case: no layout given, do nothing
            }

            // Vertex state
            pipelineDesc.vertex.module = vertexShader;
            pipelineDesc.vertex.entryPoint = BumpAllocateStringView(
                ref buffer,
                descriptor.Vertex.ProgrammableStage.EntryPoint);

            WgpuProgrammableConstants(
                ref buffer,
                descriptor.Vertex.ProgrammableStage.Constants,
                out pipelineDesc.vertex.constantCount,
                out pipelineDesc.vertex.constants);

            var buffers = descriptor.Vertex.Buffers;
            pipelineDesc.vertex.bufferCount = (uint)buffers.Length;
            pipelineDesc.vertex.buffers = BumpAllocate<WGPUVertexBufferLayout>(ref buffer, buffers.Length);
            for (var i = 0; i < buffers.Length; i++)
            {
                ref var bufferLayout = ref pipelineDesc.vertex.buffers[i];
                bufferLayout.arrayStride = buffers[i].ArrayStride;
                bufferLayout.stepMode = buffers[i].StepMode == RhiVertexStepMode.Instance
                    ? WGPUVertexStepMode.WGPUVertexStepMode_Instance
                    : WGPUVertexStepMode.WGPUVertexStepMode_Vertex;

                var attributes = buffers[i].Attributes;
                bufferLayout.attributeCount = (uint)attributes.Length;
                bufferLayout.attributes = BumpAllocate<WGPUVertexAttribute>(ref buffer, attributes.Length);
                for (var j = 0; j < attributes.Length; j++)
                {
                    ref var attribute = ref bufferLayout.attributes[j];
                    attribute.format = (WGPUVertexFormat)attributes[j].Format;
                    attribute.offset = attributes[j].Offset;
                    attribute.shaderLocation = attributes[j].ShaderLocation;
                }
            }

            // Primitive state
            pipelineDesc.primitive.topology = (WGPUPrimitiveTopology)descriptor.Primitive.Topology;
            pipelineDesc.primitive.stripIndexFormat = (WGPUIndexFormat)descriptor.Primitive.StripIndexformat;
            pipelineDesc.primitive.frontFace = (WGPUFrontFace)descriptor.Primitive.FrontFace;
            pipelineDesc.primitive.cullMode = (WGPUCullMode)descriptor.Primitive.CullMode;
            pipelineDesc.primitive.unclippedDepth = descriptor.Primitive.UnclippedDepth ? 1u : 0u;

            // Depth stencil state
            if (descriptor.DepthStencil is { } depthStencil)
            {
                var pDepthStencil = BumpAllocate<WGPUDepthStencilState>(ref buffer);
                pipelineDesc.depthStencil = pDepthStencil;

                pDepthStencil->format = (WGPUTextureFormat)depthStencil.Format;
                pDepthStencil->depthWriteEnabled = WgpuOptionalBool(depthStencil.DepthWriteEnabled);
                pDepthStencil->depthCompare = (WGPUCompareFunction)depthStencil.DepthCompare;
                pDepthStencil->stencilFront = WgpuStencilFaceState(depthStencil.StencilFront ?? new RhiStencilFaceState());
                pDepthStencil->stencilBack = WgpuStencilFaceState(depthStencil.StencilBack ?? new RhiStencilFaceState());
                pDepthStencil->stencilReadMask = depthStencil.StencilReadMask;
                pDepthStencil->stencilWriteMask = depthStencil.StencilWriteMask;
                pDepthStencil->depthBias = depthStencil.DepthBias;
                pDepthStencil->depthBiasSlopeScale = depthStencil.DepthBiasSlopeScale;
                pDepthStencil->depthBiasClamp = depthStencil.DepthBiasClamp;
            }

            // Multisample state
            pipelineDesc.multisample.count = descriptor.Multisample.Count;
            pipelineDesc.multisample.mask = descriptor.Multisample.Mask;
            pipelineDesc.multisample.alphaToCoverageEnabled = descriptor.Multisample.AlphaToCoverageEnabled ? 1u : 0u;

            // Fragment state
            if (descriptor.Fragment is { } fragment)
            {
                var fragmentShader = _shaderModuleRegistry[fragment.ProgrammableStage.ShaderModule.Handle].Native;

                var pFragment = BumpAllocate<WGPUFragmentState>(ref buffer);
                pipelineDesc.fragment = pFragment;

                pFragment->module = fragmentShader;
                pFragment->entryPoint = BumpAllocateStringView(ref buffer, fragment.ProgrammableStage.EntryPoint);

                WgpuProgrammableConstants(
                    ref buffer,
                    fragment.ProgrammableStage.Constants,
                    out pFragment->constantCount,
                    out pFragment->constants);

                var targets = fragment.Targets;
                pFragment->targetCount = (uint)targets.Length;
                pFragment->targets = BumpAllocate<WGPUColorTargetState>(ref buffer, targets.Length);
                for (var i = 0; i < targets.Length; i++)
                {
                    ref var target = ref pFragment->targets[i];
                    target.format = (WGPUTextureFormat)targets[i].Format;

                    if (targets[i].Blend is { } blend)
                    {
                        var pBlend = BumpAllocate<WGPUBlendState>(ref buffer);
                        target.blend = pBlend;

                        pBlend->alpha = WgpuBlendComponent(blend.Alpha);
                        pBlend->color = WgpuBlendComponent(blend.Color);
                    }

                    target.writeMask = (ulong)targets[i].WriteMask;
                }
            }

            nativePipeline = wgpuDeviceCreateRenderPipeline(_wgpuDevice, &pipelineDesc);
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

    private static WGPUStencilFaceState WgpuStencilFaceState(in RhiStencilFaceState state)
    {
        return new WGPUStencilFaceState
        {
            compare = (WGPUCompareFunction)state.Compare,
            failOp = (WGPUStencilOperation)state.FailOp,
            depthFailOp = (WGPUStencilOperation)state.DepthFailOp,
            passOp = (WGPUStencilOperation)state.PassOp
        };
    }

    private static void WgpuProgrammableConstants(
        ref Span<byte> buffer,
        RhiConstantEntry[] constants,
        out nuint constantCount,
        out WGPUConstantEntry* pConstants)
    {
        constantCount = (uint)constants.Length;
        pConstants = BumpAllocate<WGPUConstantEntry>(ref buffer, constants.Length);
        for (var i = 0; i < constants.Length; i++)
        {
            ref var constant = ref pConstants[i];
            constant.key = BumpAllocateStringView(ref buffer, constants[i].Key);
            constant.value = constants[i].Value;
        }
    }

    private static WGPUBlendComponent WgpuBlendComponent(in RhiBlendComponent component)
    {
        return new WGPUBlendComponent
        {
            operation = (WGPUBlendOperation)component.Operation,
            dstFactor = (WGPUBlendFactor)component.DstFactor,
            srcFactor = (WGPUBlendFactor)component.SrcFactor,
        };
    }

    private sealed class RenderPipelineReg
    {
        public WGPURenderPipeline Native;
    }

    private sealed class PipelineLayoutReg
    {
        public WGPUPipelineLayout Native;
    }
}
