namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct ogg_page
{
    [NativeTypeName("unsigned char *")]
    public byte* header;

    [NativeTypeName("long")]
    public CLong header_len;

    [NativeTypeName("unsigned char *")]
    public byte* body;

    [NativeTypeName("long")]
    public CLong body_len;
}
