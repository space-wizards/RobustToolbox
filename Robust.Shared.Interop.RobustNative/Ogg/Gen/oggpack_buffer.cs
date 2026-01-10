namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct oggpack_buffer
{
    [NativeTypeName("long")]
    public CLong endbyte;

    public int endbit;

    [NativeTypeName("unsigned char *")]
    public byte* buffer;

    [NativeTypeName("unsigned char *")]
    public byte* ptr;

    [NativeTypeName("long")]
    public CLong storage;
}
