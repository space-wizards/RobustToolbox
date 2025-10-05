namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUQueueWorkDoneCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPUQueueWorkDoneCallback")]
    public delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
