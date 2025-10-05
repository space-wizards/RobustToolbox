namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBindGroupDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("WGPUBindGroupLayout")]
    public WGPUBindGroupLayoutImpl* layout;

    [NativeTypeName("size_t")]
    public nuint entryCount;

    [NativeTypeName("const WGPUBindGroupEntry *")]
    public WGPUBindGroupEntry* entries;
}
