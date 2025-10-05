namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUFutureWaitInfo
{
    public WGPUFuture future;

    [NativeTypeName("WGPUBool")]
    public uint completed;
}
