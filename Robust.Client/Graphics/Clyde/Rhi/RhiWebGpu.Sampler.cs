using System.Collections.Generic;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, SamplerReg> _samplerRegistry = new();

    public override RhiSampler CreateSampler(in RhiSamplerDescriptor descriptor)
    {
        var addressModeU = ValidateAddressMode(descriptor.AddressModeU);
        var addressModeV = ValidateAddressMode(descriptor.AddressModeV);
        var addressModeW = ValidateAddressMode(descriptor.AddressModeW);
        var magFilter = ValidateFilterMode(descriptor.MagFilter);
        var minFilter = ValidateFilterMode(descriptor.MinFilter);
        var mipmapFilter = ValidateMipmapFilterMode(descriptor.MipmapFilter);
        var compare = ValidateCompareFunction(descriptor.Compare);

        Sampler* sampler;
        fixed (byte* label = MakeLabel(descriptor.Label))
        {
            var samplerDesc = new SamplerDescriptor
            {
                AddressModeU = (AddressMode) addressModeU,
                AddressModeV = (AddressMode) addressModeV,
                AddressModeW = (AddressMode) addressModeW,
                MagFilter = (FilterMode) magFilter,
                MinFilter = (FilterMode) minFilter,
                MipmapFilter = (MipmapFilterMode) mipmapFilter,
                LodMinClamp = descriptor.LodMinClamp,
                LodMaxClamp = descriptor.LodMaxClamp,
                Compare = (CompareFunction) compare,
                MaxAnisotropy = descriptor.MaxAnisotropy,
                Label = label
            };

            sampler = _webGpu.DeviceCreateSampler(_wgpuDevice, &samplerDesc);
        }

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _samplerRegistry.Add(handle, new SamplerReg { Native = sampler });
        return new RhiSampler(this, handle);
    }

    private sealed class SamplerReg
    {
        public Sampler* Native;
    }
}
