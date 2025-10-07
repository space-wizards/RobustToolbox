namespace Robust.Client.Graphics.Rhi.WebGpu;

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

        WGPUSampler sampler;
        fixed (byte* label = MakeLabel(descriptor.Label))
        {
            var samplerDesc = new WGPUSamplerDescriptor
            {
                addressModeU = (WGPUAddressMode) addressModeU,
                addressModeV = (WGPUAddressMode) addressModeV,
                addressModeW = (WGPUAddressMode) addressModeW,
                magFilter = (WGPUFilterMode) magFilter,
                minFilter = (WGPUFilterMode) minFilter,
                mipmapFilter = (WGPUMipmapFilterMode) mipmapFilter,
                lodMinClamp = descriptor.LodMinClamp,
                lodMaxClamp = descriptor.LodMaxClamp,
                compare = (WGPUCompareFunction) compare,
                maxAnisotropy = descriptor.MaxAnisotropy,
                label = new WGPUStringView
                {
                    data = (sbyte*)label,
                    length = WGPU_STRLEN
                }
            };

            sampler = wgpuDeviceCreateSampler(_wgpuDevice, &samplerDesc);
        }

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _samplerRegistry.Add(handle, new SamplerReg { Native = sampler });
        return new RhiSampler(this, handle);
    }

    private sealed class SamplerReg
    {
        public WGPUSampler Native;
    }
}
