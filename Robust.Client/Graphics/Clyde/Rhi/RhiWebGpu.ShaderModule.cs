using System.Collections.Generic;
using System.Text;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, ShaderModuleReg> _shaderModuleRegistry = new();

    public override RhiShaderModule CreateShaderModule(in RhiShaderModuleDescriptor descriptor)
    {
        var codeBytes = Encoding.UTF8.GetBytes(descriptor.Code);

        ShaderModule* shaderModule;
        fixed (byte* pCode = codeBytes)
        fixed (byte* pLabel = MakeLabel(descriptor.Label))
        {
            var descWgsl = new ShaderModuleWGSLDescriptor();
            descWgsl.Code = pCode;
            descWgsl.Chain.SType = SType.ShaderModuleWgsldescriptor;

            var desc = new ShaderModuleDescriptor();
            desc.Label = pLabel;
            desc.NextInChain = (ChainedStruct*) (&descWgsl);

            shaderModule = _webGpu.DeviceCreateShaderModule(_wgpuDevice, &desc);
        }

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _shaderModuleRegistry.Add(handle, new ShaderModuleReg { Native = shaderModule });
        return new RhiShaderModule(this, handle);
    }

    private sealed class ShaderModuleReg
    {
        public ShaderModule* Native;
    }
}
