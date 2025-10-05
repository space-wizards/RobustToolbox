namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUDeviceLostCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPUDeviceLostCallback")]
    public delegate* unmanaged[Cdecl]<WGPUDeviceImpl**, WGPUDeviceLostReason, WGPUStringView, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
