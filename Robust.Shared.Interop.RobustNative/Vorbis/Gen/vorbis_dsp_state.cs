namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal unsafe partial struct vorbis_dsp_state
{
    public int analysisp;

    public vorbis_info* vi;

    public float** pcm;

    public float** pcmret;

    public int pcm_storage;

    public int pcm_current;

    public int pcm_returned;

    public int preextrapolate;

    public int eofflag;

    [NativeTypeName("long")]
    public CLong lW;

    [NativeTypeName("long")]
    public CLong W;

    [NativeTypeName("long")]
    public CLong nW;

    [NativeTypeName("long")]
    public CLong centerW;

    [NativeTypeName("ogg_int64_t")]
    public CLong granulepos;

    [NativeTypeName("ogg_int64_t")]
    public CLong sequence;

    [NativeTypeName("ogg_int64_t")]
    public CLong glue_bits;

    [NativeTypeName("ogg_int64_t")]
    public CLong time_bits;

    [NativeTypeName("ogg_int64_t")]
    public CLong floor_bits;

    [NativeTypeName("ogg_int64_t")]
    public CLong res_bits;

    public void* backend_state;
}
