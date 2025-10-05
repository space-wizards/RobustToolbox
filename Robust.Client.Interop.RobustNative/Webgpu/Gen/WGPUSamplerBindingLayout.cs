namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSamplerBindingLayout
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUSamplerBindingType type;
}
