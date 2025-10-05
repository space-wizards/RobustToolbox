namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSurfaceSourceXlibWindow
{
    public WGPUChainedStruct chain;

    public void* display;

    [NativeTypeName("uint64_t")]
    public ulong window;
}
