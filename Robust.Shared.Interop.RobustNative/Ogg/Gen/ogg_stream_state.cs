namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct ogg_stream_state
{
    [NativeTypeName("unsigned char *")]
    public byte* body_data;

    [NativeTypeName("long")]
    public CLong body_storage;

    [NativeTypeName("long")]
    public CLong body_fill;

    [NativeTypeName("long")]
    public CLong body_returned;

    public int* lacing_vals;

    [NativeTypeName("ogg_int64_t *")]
    public CLong* granule_vals;

    [NativeTypeName("long")]
    public CLong lacing_storage;

    [NativeTypeName("long")]
    public CLong lacing_fill;

    [NativeTypeName("long")]
    public CLong lacing_packet;

    [NativeTypeName("long")]
    public CLong lacing_returned;

    [NativeTypeName("unsigned char[282]")]
    public fixed byte header[282];

    public int header_fill;

    public int e_o_s;

    public int b_o_s;

    [NativeTypeName("long")]
    public CLong serialno;

    [NativeTypeName("long")]
    public CLong pageno;

    [NativeTypeName("ogg_int64_t")]
    public CLong packetno;

    [NativeTypeName("ogg_int64_t")]
    public CLong granulepos;
}
