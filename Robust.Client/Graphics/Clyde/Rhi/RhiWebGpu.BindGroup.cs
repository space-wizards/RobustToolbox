using System;
using System.Collections.Generic;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, BindGroupLayoutReg> _bindGroupLayoutRegistry = new();
    private readonly Dictionary<RhiHandle, BindGroupReg> _bindGroupRegistry = new();


    internal override void BindGroupDrop(RhiBindGroup rhiBindGroup)
    {
        _wgpu.BindGroupDrop(_bindGroupRegistry[rhiBindGroup.Handle].Native);
        _bindGroupRegistry.Remove(rhiBindGroup.Handle);
    }

    public override RhiBindGroupLayout CreateBindGroupLayout(in RhiBindGroupLayoutDescriptor descriptor)
    {
        Span<byte> buffer = stackalloc byte[512];

        var pDescriptor = BumpAllocate<BindGroupLayoutDescriptor>(ref buffer);
        pDescriptor->Label = BumpAllocateUtf8(ref buffer, descriptor.Label);
        var entries = descriptor.Entries;
        pDescriptor->EntryCount = (uint)entries.Length;
        pDescriptor->Entries = BumpAllocate<BindGroupLayoutEntry>(ref buffer, entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];
            var pEntry = &pDescriptor->Entries[i];

            pEntry->Binding = entry.Binding;
            pEntry->Visibility = (ShaderStage)entry.Visibility;

            switch (entry.Layout)
            {
                case RhiSamplerBindingLayout sampler:
                    pEntry->Sampler.Type = (SamplerBindingType)sampler.Type;
                    break;
                case RhiTextureBindingLayout texture:
                    pEntry->Texture.Multisampled = texture.Multisampled;
                    pEntry->Texture.SampleType = (TextureSampleType)texture.SampleType;
                    pEntry->Texture.ViewDimension =
                        (TextureViewDimension)ValidateTextureViewDimension(texture.ViewDimension);
                    break;
                case RhiBufferBindingLayout layoutBuffer:
                    pEntry->Buffer.Type = (BufferBindingType) layoutBuffer.Type;
                    pEntry->Buffer.HasDynamicOffset = layoutBuffer.HasDynamicOffset;
                    pEntry->Buffer.MinBindingSize = layoutBuffer.MinBindingSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var native = _webGpu.DeviceCreateBindGroupLayout(_wgpuDevice, pDescriptor);

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _bindGroupLayoutRegistry.Add(handle, new BindGroupLayoutReg { Native = native });
        return new RhiBindGroupLayout(this, handle);
    }

    public override RhiBindGroup CreateBindGroup(in RhiBindGroupDescriptor descriptor)
    {
        // TODO: SAFETY
        Span<byte> buffer = stackalloc byte[1024];

        var pDescriptor = BumpAllocate<BindGroupDescriptor>(ref buffer);
        pDescriptor->Label = BumpAllocateUtf8(ref buffer, descriptor.Label);
        pDescriptor->Layout = _bindGroupLayoutRegistry[descriptor.Layout.Handle].Native;

        var entries = descriptor.Entries;
        pDescriptor->EntryCount = (uint) entries.Length;
        pDescriptor->Entries = BumpAllocate<BindGroupEntry>(ref buffer, entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref descriptor.Entries[i];
            var pEntry = &pDescriptor->Entries[i];

            pEntry->Binding = entry.Binding;
            switch (entry.Resource)
            {
                case RhiSampler rhiSampler:
                    pEntry->Sampler = _samplerRegistry[rhiSampler.Handle].Native;
                    break;
                case RhiTextureView rhiTextureView:
                    pEntry->TextureView = _textureViewRegistry[rhiTextureView.Handle].Native;
                    break;
                case RhiBufferBinding bufferBinding:
                    pEntry->Buffer = _bufferRegistry[bufferBinding.Buffer.Handle].Native;
                    pEntry->Offset = bufferBinding.Offset;
                    pEntry->Size = bufferBinding.Size ?? WebGPU.WholeSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var bindGroup = _webGpu.DeviceCreateBindGroup(_wgpuDevice, pDescriptor);

        var handle = AllocRhiHandle();
        _bindGroupRegistry.Add(handle, new BindGroupReg { Native = bindGroup });
        return new RhiBindGroup(this, handle);
    }

    private sealed class BindGroupLayoutReg
    {
        public BindGroupLayout* Native;
    }

    private sealed class BindGroupReg
    {
        public BindGroup* Native;
    }
}
