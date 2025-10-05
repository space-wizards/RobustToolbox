namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUTexelCopyBufferLayout
{
    [NativeTypeName("uint64_t")]
    public ulong offset;

    [NativeTypeName("uint32_t")]
    public uint bytesPerRow;

    [NativeTypeName("uint32_t")]
    public uint rowsPerImage;
}
