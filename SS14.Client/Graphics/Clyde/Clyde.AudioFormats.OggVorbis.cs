using System;
using System.IO;
using System.Runtime.InteropServices;
using SS14.Client.Utility;
using LibV = SS14.Client.Utility.LibVorbisFile;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private unsafe OggVorbisData _readOggVorbis(Stream stream)
        {
            // TODO: verify whether this works if the input does not support seeking.
            // I think it might just not.

            // OggVorbis_File is absolutely massive.
            // I'm too lazy to copy over that struct definition.
            // Just allocate 1kb, should be enough to fit it.
            var file = (LibVorbisFile.OggVorbis_File*) 0;
            try
            {
                file = (LibVorbisFile.OggVorbis_File*) Marshal.AllocHGlobal(1024);
                var span = new Span<byte>((byte*) file, 1024);
                var callbacks = new LibVorbisFile.ov_callbacks();
                callbacks.close_func = (void*) 0;

                Func<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> readFunc = (ptr, size, nmemb, datasource) =>
                {
                    var read = 0;
                    var readBuffer = new byte[(long) size * (long) nmemb];
                    while (read < readBuffer.Length)
                    {
                        var ret = stream.Read(readBuffer, read, readBuffer.Length - read);
                        if (ret == 0)
                        {
                            break;
                        }

                        read += ret;
                    }

                    Marshal.Copy(readBuffer, 0, ptr, readBuffer.Length);
                    return (IntPtr) readBuffer.Length;
                };

                callbacks.read_func = (void*) Marshal.GetFunctionPointerForDelegate(readFunc);

                if (stream.CanSeek)
                {
                    Func<IntPtr, long, int, int> seekFunc = (datasource, offset, whence) =>
                    {
                        var origin = (SeekOrigin) whence;
                        try
                        {
                            stream.Seek(offset, origin);
                        }
                        catch (Exception)
                        {
                            return -1;
                        }

                        return 0;
                    };

                    Func<IntPtr, long> tellFunc = datasource => stream.Position;

                    callbacks.tell_func = (void*) Marshal.GetFunctionPointerForDelegate(tellFunc);
                    callbacks.seek_func = (void*) Marshal.GetFunctionPointerForDelegate(seekFunc);
                }

                var error = LibV.ov_open_callbacks((void*) 1, file, (byte*) 0, 0, callbacks);
                if (error != 0)
                {
                    throw new InvalidOperationException();
                }

                // Just read the first stream and be done with it.
                if (LibV.ov_streams(file) != 1)
                {
                    // Too lazy to test, just blow it up.
                    throw new InvalidOperationException("Different amount of streams than 1, I don't trust this.");
                }
                var info = LibV.ov_info(file, -1);

                var sampleRate = info->rate;
                var channels = info->channels;
                var totalSamples = LibV.ov_pcm_total(file, -1);

                var sampleSize = channels * 2;
                var totalBytes = sampleSize * totalSamples;

                var readBytes = 0;
                var buffer = new byte[totalBytes];
                var bitStream = 0;

                while (readBytes < buffer.Length)
                {
                    fixed (byte* bufPtr = buffer)
                    {
                        var ret = LibV.ov_read(file, bufPtr+readBytes, buffer.Length-readBytes, 0, 2, 0, &bitStream);
                        if (ret < 0)
                        {
                            throw new InvalidOperationException();
                        }

                        if (ret == 0)
                        {
                            break;
                        }

                        readBytes += (int)ret;
                    }
                }

                return new OggVorbisData(totalSamples, sampleRate, channels, buffer);
            }
            finally
            {
                LibV.ov_clear(file);
                Marshal.FreeHGlobal((IntPtr) file);
            }
        }

        private readonly struct OggVorbisData
        {
            public readonly long TotalSamples;
            public readonly long SampleRate;
            public readonly long Channels;
            public readonly ReadOnlyMemory<byte> Data;

            public OggVorbisData(long totalSamples, long sampleRate, long channels, ReadOnlyMemory<byte> data)
            {
                TotalSamples = totalSamples;
                SampleRate = sampleRate;
                Channels = channels;
                Data = data;
            }
        }
    }
}
