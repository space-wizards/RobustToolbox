namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBufferMapCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPUBufferMapCallback")]
    public delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
