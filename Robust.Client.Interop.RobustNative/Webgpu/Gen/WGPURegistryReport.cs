namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPURegistryReport
{
    [NativeTypeName("size_t")]
    public nuint numAllocated;

    [NativeTypeName("size_t")]
    public nuint numKeptFromUser;

    [NativeTypeName("size_t")]
    public nuint numReleasedFromUser;

    [NativeTypeName("size_t")]
    public nuint elementSize;
}
