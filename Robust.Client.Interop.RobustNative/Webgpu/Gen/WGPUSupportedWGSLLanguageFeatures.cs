namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSupportedWGSLLanguageFeatures
{
    [NativeTypeName("size_t")]
    public nuint featureCount;

    [NativeTypeName("const WGPUWGSLLanguageFeatureName *")]
    public WGPUWGSLLanguageFeatureName* features;
}
