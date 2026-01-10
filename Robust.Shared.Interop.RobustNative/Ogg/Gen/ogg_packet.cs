namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct ogg_packet
{
    [NativeTypeName("unsigned char *")]
    public byte* packet;

    [NativeTypeName("long")]
    public CLong bytes;

    [NativeTypeName("long")]
    public CLong b_o_s;

    [NativeTypeName("long")]
    public CLong e_o_s;

    [NativeTypeName("ogg_int64_t")]
    public CLong granulepos;

    [NativeTypeName("ogg_int64_t")]
    public CLong packetno;
}
