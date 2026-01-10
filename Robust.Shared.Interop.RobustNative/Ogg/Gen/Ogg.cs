using System.Runtime.InteropServices;

namespace Robust.Shared.Interop.RobustNative.Ogg;

internal static unsafe partial class Ogg
{
    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_writeinit(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int oggpack_writecheck(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_writetrunc(oggpack_buffer* b, [NativeTypeName("long")] CLong bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_writealign(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_writecopy(oggpack_buffer* b, void* source, [NativeTypeName("long")] CLong bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_reset(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_writeclear(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_readinit(oggpack_buffer* b, [NativeTypeName("unsigned char *")] byte* buf, int bytes);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_write(oggpack_buffer* b, [NativeTypeName("unsigned long")] CULong value, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_look(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_look1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_adv(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpack_adv1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_read(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_read1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_bytes(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpack_bits(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("unsigned char *")]
    public static extern byte* oggpack_get_buffer(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_writeinit(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int oggpackB_writecheck(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_writetrunc(oggpack_buffer* b, [NativeTypeName("long")] CLong bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_writealign(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_writecopy(oggpack_buffer* b, void* source, [NativeTypeName("long")] CLong bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_reset(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_writeclear(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_readinit(oggpack_buffer* b, [NativeTypeName("unsigned char *")] byte* buf, int bytes);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_write(oggpack_buffer* b, [NativeTypeName("unsigned long")] CULong value, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_look(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_look1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_adv(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void oggpackB_adv1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_read(oggpack_buffer* b, int bits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_read1(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_bytes(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong oggpackB_bits(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("unsigned char *")]
    public static extern byte* oggpackB_get_buffer(oggpack_buffer* b);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_packetin(ogg_stream_state* os, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_iovecin(ogg_stream_state* os, ogg_iovec_t* iov, int count, [NativeTypeName("long")] CLong e_o_s, [NativeTypeName("ogg_int64_t")] CLong granulepos);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_pageout(ogg_stream_state* os, ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_pageout_fill(ogg_stream_state* os, ogg_page* og, int nfill);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_flush(ogg_stream_state* os, ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_flush_fill(ogg_stream_state* os, ogg_page* og, int nfill);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_init(ogg_sync_state* oy);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_clear(ogg_sync_state* oy);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_reset(ogg_sync_state* oy);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_destroy(ogg_sync_state* oy);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_check(ogg_sync_state* oy);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* ogg_sync_buffer(ogg_sync_state* oy, [NativeTypeName("long")] CLong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_wrote(ogg_sync_state* oy, [NativeTypeName("long")] CLong bytes);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong ogg_sync_pageseek(ogg_sync_state* oy, ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_sync_pageout(ogg_sync_state* oy, ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_pagein(ogg_stream_state* os, ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_packetout(ogg_stream_state* os, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_packetpeek(ogg_stream_state* os, ogg_packet* op);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_init(ogg_stream_state* os, int serialno);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_clear(ogg_stream_state* os);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_reset(ogg_stream_state* os);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_reset_serialno(ogg_stream_state* os, int serialno);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_destroy(ogg_stream_state* os);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_check(ogg_stream_state* os);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_stream_eos(ogg_stream_state* os);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ogg_page_checksum_set(ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_version([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_continued([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_bos([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_eos([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ogg_int64_t")]
    public static extern CLong ogg_page_granulepos([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_serialno([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("long")]
    public static extern CLong ogg_page_pageno([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int ogg_page_packets([NativeTypeName("const ogg_page *")] ogg_page* og);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ogg_packet_clear(ogg_packet* op);
}
