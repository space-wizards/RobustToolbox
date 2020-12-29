using System;
using System.IO;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private OggVorbisData _readOggVorbis(Stream stream)
        {
            using (var vorbis = new NVorbis.VorbisReader(stream, false))
            {
                var sampleRate = vorbis.SampleRate;
                var channels = vorbis.Channels;
                var totalSamples = vorbis.TotalSamples;

                var readSamples = 0;
                var buffer = new float[totalSamples * channels];

                while (readSamples < totalSamples)
                {
                    var read = vorbis.ReadSamples(buffer, readSamples * channels, buffer.Length - readSamples);
                    if (read == 0)
                    {
                        break;
                    }

                    readSamples += read;
                }

                return new OggVorbisData(totalSamples, sampleRate, channels, buffer);
            }
        }

        private readonly struct OggVorbisData
        {
            public readonly long TotalSamples;
            public readonly long SampleRate;
            public readonly long Channels;
            public readonly ReadOnlyMemory<float> Data;

            public OggVorbisData(long totalSamples, long sampleRate, long channels, ReadOnlyMemory<float> data)
            {
                TotalSamples = totalSamples;
                SampleRate = sampleRate;
                Channels = channels;
                Data = data;
            }
        }
    }
}
