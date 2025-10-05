namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUDeviceDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("size_t")]
    public nuint requiredFeatureCount;

    [NativeTypeName("const WGPUFeatureName *")]
    public WGPUFeatureName* requiredFeatures;

    [NativeTypeName("const WGPULimits *")]
    public WGPULimits* requiredLimits;

    public WGPUQueueDescriptor defaultQueue;

    public WGPUDeviceLostCallbackInfo deviceLostCallbackInfo;

    public WGPUUncapturedErrorCallbackInfo uncapturedErrorCallbackInfo;
}
