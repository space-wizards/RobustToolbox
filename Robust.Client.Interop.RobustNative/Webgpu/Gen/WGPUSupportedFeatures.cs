namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSupportedFeatures
{
    [NativeTypeName("size_t")]
    public nuint featureCount;

    [NativeTypeName("const WGPUFeatureName *")]
    public WGPUFeatureName* features;
}
