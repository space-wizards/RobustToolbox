using System;
using System.IO;
using System.Numerics;
using NVorbis;

namespace Robust.Shared.Audio.AudioLoading;

/// <summary>
/// Implements functionality for loading ogg audio files.
/// </summary>
/// <seealso cref="AudioLoaderOgg"/>
internal static class AudioLoaderOgg
{
    private const int ReadBufferLength = short.MaxValue + 1;

    /// <summary>
    /// Load metadata for an ogg audio file.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static AudioMetadata LoadAudioMetadata(Stream stream)
    {
        using var reader = new VorbisReader(stream, false);
        reader.Initialize();
        return new AudioMetadata(reader.TotalTime, reader.Channels, reader.Tags.Title, reader.Tags.Artist);
    }

    /// <summary>
    /// Load an ogg file into raw samples and metadata.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static OggVorbisData LoadAudioData(Stream stream)
    {
        using var vorbis = new VorbisReader(stream, false);
        vorbis.Initialize();

        var sampleRate = vorbis.SampleRate;
        var channels = vorbis.Channels;
        var totalSamples = vorbis.TotalSamples;

        var totalValues = totalSamples * channels;
        var readValues = 0;
        var buffer = new short[totalSamples * channels];
        Span<float> readBuffer = stackalloc float[ReadBufferLength];

        while (readValues < totalValues)
        {
            var read = ReadSamples(buffer.AsSpan(readValues), readBuffer, channels, vorbis);
            if (read == 0)
                break;

            readValues += read;
        }

        return new OggVorbisData(totalSamples, sampleRate, channels, buffer, vorbis.Tags.Title, vorbis.Tags.Artist);
    }

    private static int ReadSamples(Span<short> dest, Span<float> readBuffer, int channels, VorbisReader reader)
    {
        // Work around https://github.com/space-wizards/space-station-14/issues/22676
        // This is a bug in NVorbis/VorbisPizza: some malformed ogg vorbis files cause an infinite loop.
        // On the problematic files we've run into, making sure we only ask VorbisReader
        // for exactly as many samples as reported avoids this.
        if (readBuffer.Length > dest.Length)
            readBuffer = readBuffer[..dest.Length];

        var read = reader.ReadSamples(readBuffer);
        read *= channels;

        ConvertToShort(readBuffer[..read], dest[..read]);

        return read;
    }

    private static void ConvertToShort(ReadOnlySpan<float> src, Span<short> dst)
    {
        if (src.Length != dst.Length)
            throw new InvalidOperationException("Invalid lengths!");

        var simdSamples = (src.Length / Vector<short>.Count) * Vector<short>.Count;

        // Note: I think according to spec we'd actually need to multiply negative values with 2^15 instead of 2^15-1.
        // because it's -32768 -> 32767
        // Can't be arsed though
        var factor = new Vector<float>(short.MaxValue);

        ref readonly var srcBase = ref src[0];
        ref var dstBase = ref dst[0];

        for (var i = 0; i < simdSamples; i += Vector<short>.Count)
        {
            var lower = Vector.LoadUnsafe(in srcBase, (nuint) i);
            var upper = Vector.LoadUnsafe(in srcBase, (nuint) (i + Vector<float>.Count));

            lower *= factor;
            upper *= factor;

            var lowerInt = Vector.ConvertToInt32(lower);
            var upperInt = Vector.ConvertToInt32(upper);

            var merged = Vector.Narrow(lowerInt, upperInt);

            merged.StoreUnsafe(ref dstBase, (nuint) i);
        }

        for (var i = simdSamples; i < src.Length; i++)
        {
            dst[i] = (short)(src[i] * short.MaxValue);
        }
    }

    internal readonly struct OggVorbisData
    {
        public readonly long TotalSamples;
        public readonly long SampleRate;
        public readonly long Channels;
        public readonly ReadOnlyMemory<short> Data;
        public readonly string Title;
        public readonly string Artist;

        public OggVorbisData(long totalSamples, long sampleRate, long channels, ReadOnlyMemory<short> data,
            string title, string artist)
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
