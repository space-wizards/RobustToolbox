namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBindGroupLayoutDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("size_t")]
    public nuint entryCount;

    [NativeTypeName("const WGPUBindGroupLayoutEntry *")]
    public WGPUBindGroupLayoutEntry* entries;
}
