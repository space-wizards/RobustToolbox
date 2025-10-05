namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUPopErrorScopeCallbackInfo
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUCallbackMode mode;

    [NativeTypeName("WGPUPopErrorScopeCallback")]
    public delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, void*, void*, void> callback;

    public void* userdata1;

    public void* userdata2;
}
