namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal unsafe partial struct vorbis_block
{
    public float** pcm;

    public oggpack_buffer opb;

    [NativeTypeName("long")]
    public CLong lW;

    [NativeTypeName("long")]
    public CLong W;

    [NativeTypeName("long")]
    public CLong nW;

    public int pcmend;

    public int mode;

    public int eofflag;

    [NativeTypeName("ogg_int64_t")]
    public CLong granulepos;

    [NativeTypeName("ogg_int64_t")]
    public CLong sequence;

    public vorbis_dsp_state* vd;

    public void* localstore;

    [NativeTypeName("long")]
    public CLong localtop;

    [NativeTypeName("long")]
    public CLong localalloc;

    [NativeTypeName("long")]
    public CLong totaluse;

    [NativeTypeName("struct alloc_chain *")]
    public alloc_chain* reap;

    [NativeTypeName("long")]
    public CLong glue_bits;

    [NativeTypeName("long")]
    public CLong time_bits;

    [NativeTypeName("long")]
    public CLong floor_bits;

    [NativeTypeName("long")]
    public CLong res_bits;

    public void* @internal;
}
