namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUNativeLimits
{
    public WGPUChainedStructOut chain;

    [NativeTypeName("uint32_t")]
    public uint maxPushConstantSize;

    [NativeTypeName("uint32_t")]
    public uint maxNonSamplerBindings;
}
