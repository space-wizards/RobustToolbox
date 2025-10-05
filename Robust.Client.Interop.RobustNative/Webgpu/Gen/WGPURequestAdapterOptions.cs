namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURequestAdapterOptions
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUFeatureLevel featureLevel;

    public WGPUPowerPreference powerPreference;

    [NativeTypeName("WGPUBool")]
    public uint forceFallbackAdapter;

    public WGPUBackendType backendType;

    [NativeTypeName("WGPUSurface")]
    public WGPUSurfaceImpl* compatibleSurface;
}
