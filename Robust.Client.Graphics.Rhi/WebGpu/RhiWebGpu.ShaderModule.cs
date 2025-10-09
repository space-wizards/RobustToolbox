using System.Text;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, ShaderModuleReg> _shaderModuleRegistry = new();

    public override RhiShaderModule CreateShaderModule(in RhiShaderModuleDescriptor descriptor)
    {
        var codeBytes = Encoding.UTF8.GetBytes(descriptor.Code);

        return CreateShaderModule(new RhiShaderModuleDescriptorUtf8
        {
            Code = codeBytes,
            Label = descriptor.Label
        });
    }

    public override RhiShaderModule CreateShaderModule(in RhiShaderModuleDescriptorUtf8 descriptor)
    {
        WGPUShaderModule shaderModule;
        fixed (byte* pCode = descriptor.Code)
        fixed (byte* pLabel = MakeLabel(descriptor.Label))
        {
            var descWgsl = new WGPUShaderSourceWGSL();
            descWgsl.code = new WGPUStringView
            {
                data = (sbyte*)pCode,
                length = WGPU_STRLEN
            };
            descWgsl.chain.sType = WGPUSType.WGPUSType_ShaderSourceWGSL;

            var desc = new WGPUShaderModuleDescriptor();
            desc.label = new WGPUStringView
            {
                data = (sbyte*)pLabel,
                length = WGPU_STRLEN
            };
            desc.nextInChain = (WGPUChainedStruct*) (&descWgsl);

            shaderModule = wgpuDeviceCreateShaderModule(_wgpuDevice, &desc);
        }

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _shaderModuleRegistry.Add(handle, new ShaderModuleReg { Native = shaderModule });
        return new RhiShaderModule(this, handle);
    }

    private sealed class ShaderModuleReg
    {
        public WGPUShaderModule Native;
    }
}
