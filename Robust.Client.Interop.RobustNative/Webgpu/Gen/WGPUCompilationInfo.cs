namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUCompilationInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("size_t")]
    public nuint messageCount;

    [NativeTypeName("const WGPUCompilationMessage *")]
    public WGPUCompilationMessage* messages;
}
