using System.Runtime.InteropServices;

namespace Robust.Shared.Interop.RobustNative.Opus;

internal static unsafe partial class Opus
{
    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const char *")]
    public static extern sbyte* opus_strerror(int error);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const char *")]
    public static extern sbyte* opus_get_version_string();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_encoder_get_size(int channels);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusEncoder* opus_encoder_create([NativeTypeName("opus_int32")] int Fs, int channels, int application, int* error);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_encoder_init(OpusEncoder* st, [NativeTypeName("opus_int32")] int Fs, int channels, int application);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_encode(OpusEncoder* st, [NativeTypeName("const opus_int16 *")] short* pcm, int frame_size, [NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int max_data_bytes);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_encode_float(OpusEncoder* st, [NativeTypeName("const float *")] float* pcm, int frame_size, [NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int max_data_bytes);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_encoder_destroy(OpusEncoder* st);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_encoder_ctl(OpusEncoder* st, int request, __arglist);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_get_size(int channels);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusDecoder* opus_decoder_create([NativeTypeName("opus_int32")] int Fs, int channels, int* error);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_init(OpusDecoder* st, [NativeTypeName("opus_int32")] int Fs, int channels);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decode(OpusDecoder* st, [NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, [NativeTypeName("opus_int16 *")] short* pcm, int frame_size, int decode_fec);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decode_float(OpusDecoder* st, [NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, float* pcm, int frame_size, int decode_fec);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_ctl(OpusDecoder* st, int request, __arglist);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_decoder_destroy(OpusDecoder* st);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_decoder_get_size();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusDREDDecoder* opus_dred_decoder_create(int* error);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_decoder_init(OpusDREDDecoder* dec);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_dred_decoder_destroy(OpusDREDDecoder* dec);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_decoder_ctl(OpusDREDDecoder* dred_dec, int request, __arglist);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_get_size();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusDRED* opus_dred_alloc(int* error);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_dred_free(OpusDRED* dec);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_parse(OpusDREDDecoder* dred_dec, OpusDRED* dred, [NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, [NativeTypeName("opus_int32")] int max_dred_samples, [NativeTypeName("opus_int32")] int sampling_rate, int* dred_end, int defer_processing);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_dred_process(OpusDREDDecoder* dred_dec, [NativeTypeName("const OpusDRED *")] OpusDRED* src, OpusDRED* dst);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_dred_decode(OpusDecoder* st, [NativeTypeName("const OpusDRED *")] OpusDRED* dred, [NativeTypeName("opus_int32")] int dred_offset, [NativeTypeName("opus_int16 *")] short* pcm, [NativeTypeName("opus_int32")] int frame_size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_dred_decode_float(OpusDecoder* st, [NativeTypeName("const OpusDRED *")] OpusDRED* dred, [NativeTypeName("opus_int32")] int dred_offset, float* pcm, [NativeTypeName("opus_int32")] int frame_size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_parse([NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, [NativeTypeName("unsigned char *")] byte* out_toc, [NativeTypeName("const unsigned char *[48]")] byte** frames, [NativeTypeName("opus_int16[48]")] short* size, int* payload_offset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_get_bandwidth([NativeTypeName("const unsigned char *")] byte* data);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_get_samples_per_frame([NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int Fs);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_get_nb_channels([NativeTypeName("const unsigned char *")] byte* data);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_get_nb_frames([NativeTypeName("const unsigned char[]")] byte* packet, [NativeTypeName("opus_int32")] int len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_get_nb_samples([NativeTypeName("const unsigned char[]")] byte* packet, [NativeTypeName("opus_int32")] int len, [NativeTypeName("opus_int32")] int Fs);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_has_lbrr([NativeTypeName("const unsigned char[]")] byte* packet, [NativeTypeName("opus_int32")] int len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_decoder_get_nb_samples([NativeTypeName("const OpusDecoder *")] OpusDecoder* dec, [NativeTypeName("const unsigned char[]")] byte* packet, [NativeTypeName("opus_int32")] int len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_pcm_soft_clip(float* pcm, int frame_size, int channels, float* softclip_mem);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_repacketizer_get_size();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusRepacketizer* opus_repacketizer_init(OpusRepacketizer* rp);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern OpusRepacketizer* opus_repacketizer_create();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void opus_repacketizer_destroy(OpusRepacketizer* rp);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_repacketizer_cat(OpusRepacketizer* rp, [NativeTypeName("const unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_repacketizer_out_range(OpusRepacketizer* rp, int begin, int end, [NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int maxlen);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_repacketizer_get_nb_frames(OpusRepacketizer* rp);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_repacketizer_out(OpusRepacketizer* rp, [NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int maxlen);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_packet_pad([NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, [NativeTypeName("opus_int32")] int new_len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_packet_unpad([NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int opus_multistream_packet_pad([NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, [NativeTypeName("opus_int32")] int new_len, int nb_streams);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("opus_int32")]
    public static extern int opus_multistream_packet_unpad([NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("opus_int32")] int len, int nb_streams);
}
