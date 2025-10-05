namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUCompilationInfoCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPUCompilationInfoCallback")]
    public delegate* unmanaged[Cdecl]<WGPUCompilationInfoRequestStatus, WGPUCompilationInfo*, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
