namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUConstantEntry
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView key;

    public double value;
}
