using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SS14.Client.Utility
{
    /// <summary>
    ///     Minimal binding to libvorbisfile.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static class LibVorbisFile
    {
        [DllImport("vorbisfile.dll")]
        public static extern unsafe int ov_open_callbacks(void* datasource, OggVorbis_File* vf, byte* initial,
            long ibytes, ov_callbacks callbacks);

        [DllImport("vorbisfile.dll")]
        public static extern unsafe int ov_clear(OggVorbis_File* vf);

        [DllImport("vorbisfile.dll")]
        public static extern unsafe long ov_streams(OggVorbis_File* vf);

        [DllImport("vorbisfile.dll")]
        public static extern unsafe vorbis_info* ov_info(OggVorbis_File* vf, int link);

        [DllImport("vorbisfile.dll")]
        public static extern unsafe long ov_pcm_total(OggVorbis_File* vf, int link);

        [DllImport("vorbisfile.dll")]
        public static extern unsafe long ov_read(OggVorbis_File* vf, byte* buffer, int length, int bigendianp, int word,
            int sgned, int* bitstream);

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct OggVorbis_File
        {
            // AHAHAHAHA this struct is ridiculously huge.
            // I'm just gonna allocate a 1 kb buffer for it instead so I don't have to worry about size.
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ov_callbacks
        {
            public void* read_func;
            public void* seek_func;
            public void* close_func;
            public void* tell_func;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct vorbis_info
        {
            public int version;
            public int channels;
            public long rate;

            public long bitrate_upper;
            public long bitrate_nominal;
            public long bitrate_lower;
            public long bitrate_window;

            public void* codec_setup;
        }
    }
}
