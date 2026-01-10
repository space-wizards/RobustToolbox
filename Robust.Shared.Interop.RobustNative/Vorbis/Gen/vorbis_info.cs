namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal unsafe partial struct vorbis_info
{
    public int version;

    public int channels;

    [NativeTypeName("long")]
    public CLong rate;

    [NativeTypeName("long")]
    public CLong bitrate_upper;

    [NativeTypeName("long")]
    public CLong bitrate_nominal;

    [NativeTypeName("long")]
    public CLong bitrate_lower;

    [NativeTypeName("long")]
    public CLong bitrate_window;

    public void* codec_setup;
}
