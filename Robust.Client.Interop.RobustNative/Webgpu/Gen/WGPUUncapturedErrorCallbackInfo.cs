namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUUncapturedErrorCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("WGPUUncapturedErrorCallback")]
    public delegate* unmanaged[Cdecl]<WGPUDeviceImpl**, WGPUErrorType, WGPUStringView, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
