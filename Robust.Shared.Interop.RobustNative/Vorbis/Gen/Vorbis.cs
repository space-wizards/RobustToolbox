using System.Runtime.InteropServices;

namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal static unsafe partial class Vorbis
{
    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_info_init(vorbis_info* vi);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_info_clear(vorbis_info* vi);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_info_blocksize(vorbis_info* vi, int zo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_comment_init(vorbis_comment* vc);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_comment_add(vorbis_comment* vc, [NativeTypeName("const char *")] sbyte* comment);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_comment_add_tag(vorbis_comment* vc, [NativeTypeName("const char *")] sbyte* tag, [NativeTypeName("const char *")] sbyte* contents);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* vorbis_comment_query(vorbis_comment* vc, [NativeTypeName("const char *")] sbyte* tag, int count);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_comment_query_count(vorbis_comment* vc, [NativeTypeName("const char *")] sbyte* tag);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_comment_clear(vorbis_comment* vc);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_block_init(vorbis_dsp_state* v, vorbis_block* vb);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_block_clear(vorbis_block* vb);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void vorbis_dsp_clear(vorbis_dsp_state* v);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern double vorbis_granule_time(vorbis_dsp_state* v, [NativeTypeName("ogg_int64_t")] CLong granulepos);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const char *")]
    public static extern sbyte* vorbis_version_string();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_analysis_init(vorbis_dsp_state* v, vorbis_info* vi);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_commentheader_out(vorbis_comment* vc, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_analysis_headerout(vorbis_dsp_state* v, vorbis_comment* vc, ogg_packet* op, ogg_packet* op_comm, ogg_packet* op_code);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float** vorbis_analysis_buffer(vorbis_dsp_state* v, int vals);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_analysis_wrote(vorbis_dsp_state* v, int vals);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_analysis_blockout(vorbis_dsp_state* v, vorbis_block* vb);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_analysis(vorbis_block* vb, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_bitrate_addblock(vorbis_block* vb);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_bitrate_flushpacket(vorbis_dsp_state* vd, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_idheader(ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_headerin(vorbis_info* vi, vorbis_comment* vc, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_init(vorbis_dsp_state* v, vorbis_info* vi);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_restart(vorbis_dsp_state* v);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis(vorbis_block* vb, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_trackonly(vorbis_block* vb, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_blockin(vorbis_dsp_state* v, vorbis_block* vb);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_pcmout(vorbis_dsp_state* v, float*** pcm);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_lapout(vorbis_dsp_state* v, float*** pcm);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_read(vorbis_dsp_state* v, int samples);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong vorbis_packet_blocksize(vorbis_info* vi, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_halfrate(vorbis_info* v, int flag);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int vorbis_synthesis_halfrate_p(vorbis_info* v);
}
