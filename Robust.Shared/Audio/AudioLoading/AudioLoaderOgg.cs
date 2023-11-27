using System;
using System.IO;
using NVorbis;

namespace Robust.Shared.Audio.AudioLoading;

/// <summary>
/// Implements functionality for loading ogg audio files.
/// </summary>
/// <seealso cref="AudioLoaderOgg"/>
internal static class AudioLoaderOgg
{
    /// <summary>
    /// Load metadata for an ogg audio file.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static AudioMetadata LoadAudioMetadata(Stream stream)
    {
        using var reader = new VorbisReader(stream);
        return new AudioMetadata(reader.TotalTime, reader.Channels, reader.Tags.Title, reader.Tags.Artist);
    }

    /// <summary>
    /// Load an ogg file into raw samples and metadata.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static OggVorbisData LoadAudioData(Stream stream)
    {
        using var vorbis = new NVorbis.VorbisReader(stream, false);

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

        return new OggVorbisData(totalSamples, sampleRate, channels, buffer, vorbis.Tags.Title, vorbis.Tags.Artist);
    }

    internal readonly struct OggVorbisData
    {
        public readonly long TotalSamples;
        public readonly long SampleRate;
        public readonly long Channels;
        public readonly ReadOnlyMemory<float> Data;
        public readonly string Title;
        public readonly string Artist;

        public OggVorbisData(long totalSamples, long sampleRate, long channels, ReadOnlyMemory<float> data, string title, string artist)
        {
            TotalSamples = totalSamples;
            SampleRate = sampleRate;
            Channels = channels;
            Data = data;
            Title = title;
            Artist = artist;
        }
    }
}
