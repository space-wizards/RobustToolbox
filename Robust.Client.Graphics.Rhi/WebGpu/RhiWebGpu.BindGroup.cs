namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, BindGroupLayoutReg> _bindGroupLayoutRegistry = new();
    private readonly Dictionary<RhiHandle, BindGroupReg> _bindGroupRegistry = new();


    internal override void BindGroupDrop(RhiBindGroup rhiBindGroup)
    {
        wgpuBindGroupRelease(_bindGroupRegistry[rhiBindGroup.Handle].Native);
        _bindGroupRegistry.Remove(rhiBindGroup.Handle);
    }

    public override RhiBindGroupLayout CreateBindGroupLayout(in RhiBindGroupLayoutDescriptor descriptor)
    {
        Span<byte> buffer = stackalloc byte[512];

        var pDescriptor = BumpAllocate<WGPUBindGroupLayoutDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);
        var entries = descriptor.Entries;
        pDescriptor->entryCount = (uint)entries.Length;
        pDescriptor->entries = BumpAllocate<WGPUBindGroupLayoutEntry>(ref buffer, entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];
            var pEntry = &pDescriptor->entries[i];

            pEntry->binding = entry.Binding;
            pEntry->visibility = (ulong)entry.Visibility;

            switch (entry.Layout)
            {
                case RhiSamplerBindingLayout sampler:
                    pEntry->sampler.type = (WGPUSamplerBindingType)sampler.Type;
                    break;
                case RhiTextureBindingLayout texture:
                    pEntry->texture.multisampled = texture.Multisampled ? 1u : 0u;
                    pEntry->texture.sampleType = (WGPUTextureSampleType)texture.SampleType;
                    pEntry->texture.viewDimension =
                        (WGPUTextureViewDimension)ValidateTextureViewDimension(texture.ViewDimension);
                    break;
                case RhiBufferBindingLayout layoutBuffer:
                    pEntry->buffer.type = (WGPUBufferBindingType) layoutBuffer.Type;
                    pEntry->buffer.hasDynamicOffset = layoutBuffer.HasDynamicOffset ? 1u : 0u;
                    pEntry->buffer.minBindingSize = layoutBuffer.MinBindingSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var native = wgpuDeviceCreateBindGroupLayout(_wgpuDevice, pDescriptor);

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _bindGroupLayoutRegistry.Add(handle, new BindGroupLayoutReg { Native = native });
        return new RhiBindGroupLayout(this, handle);
    }

    public override RhiBindGroup CreateBindGroup(in RhiBindGroupDescriptor descriptor)
    {
        // TODO: SAFETY
        Span<byte> buffer = stackalloc byte[1024];

        var pDescriptor = BumpAllocate<WGPUBindGroupDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);
        pDescriptor->layout = _bindGroupLayoutRegistry[descriptor.Layout.Handle].Native;

        var entries = descriptor.Entries;
        pDescriptor->entryCount = (uint) entries.Length;
        pDescriptor->entries = BumpAllocate<WGPUBindGroupEntry>(ref buffer, entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref descriptor.Entries[i];
            var pEntry = &pDescriptor->entries[i];

            pEntry->binding = entry.Binding;
            switch (entry.Resource)
            {
                case RhiSampler rhiSampler:
                    pEntry->sampler = _samplerRegistry[rhiSampler.Handle].Native;
                    break;
                case RhiTextureView rhiTextureView:
                    pEntry->textureView = _textureViewRegistry[rhiTextureView.Handle].Native;
                    break;
                case RhiBufferBinding bufferBinding:
                    pEntry->buffer = _bufferRegistry[bufferBinding.Buffer.Handle].Native;
                    pEntry->offset = bufferBinding.Offset;
                    pEntry->size = bufferBinding.Size ?? WGPU_WHOLE_SIZE;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var bindGroup = wgpuDeviceCreateBindGroup(_wgpuDevice, pDescriptor);

        var handle = AllocRhiHandle();
        _bindGroupRegistry.Add(handle, new BindGroupReg { Native = bindGroup });
        return new RhiBindGroup(this, handle);
    }

    private sealed class BindGroupLayoutReg
    {
        public WGPUBindGroupLayout Native;
    }

    private sealed class BindGroupReg
    {
        public WGPUBindGroup Native;
    }
}
