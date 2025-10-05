namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURequestAdapterCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPURequestAdapterCallback")]
    public delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapterImpl*, WGPUStringView, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
