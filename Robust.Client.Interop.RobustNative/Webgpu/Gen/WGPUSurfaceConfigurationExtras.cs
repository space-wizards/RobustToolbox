namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUSurfaceConfigurationExtras
{
    public WGPUChainedStruct chain;

    [NativeTypeName("uint32_t")]
    public uint desiredMaximumFrameLatency;
}
