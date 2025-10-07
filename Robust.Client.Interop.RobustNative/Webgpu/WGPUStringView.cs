namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUStringView
{
    public static readonly WGPUStringView Null = new WGPUStringView
    {
        data = null,
        length = Wgpu.WGPU_STRLEN,
    };
}
