using System;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio;

internal abstract class SharedAudioManager
{
    #region Loading

    public virtual AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
    {
        var vorbis = _readOggVorbis(stream);
        var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double) vorbis.SampleRate);
        return new AudioStream(null, length, (int) vorbis.Channels, name, vorbis.Title, vorbis.Artist);
    }

    public virtual AudioStream LoadAudioWav(Stream stream, string? name = null)
    {
        var wav = _readWav(stream);
        var length = TimeSpan.FromSeconds(wav.Data.Length / (double) wav.BlockAlign / wav.SampleRate);
        return new AudioStream(null, length, wav.NumChannels, name);
    }

    public virtual AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
    {
        var length = TimeSpan.FromSeconds((double) samples.Length / channels / sampleRate);
        return new AudioStream(null, length, channels, name);
    }

    #endregion

    #region Loading Data

    protected OggVorbisData _readOggVorbis(Stream stream)
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

            return new OggVorbisData(totalSamples, sampleRate, channels, buffer, vorbis.Tags.Title, vorbis.Tags.Artist);
        }
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

    /// <summary>
    ///     Load up a WAVE file.
    /// </summary>
    protected static WavData _readWav(Stream stream)
    {
        var reader = new BinaryReader(stream, EncodingHelpers.UTF8, true);

        void SkipChunk()
        {
            var length = reader.ReadUInt32();
            stream.Position += length;
        }

        // Read outer most chunks.
        Span<byte> fourCc = stackalloc byte[4];
        while (true)
        {
            _readFourCC(reader, fourCc);

            if (!fourCc.SequenceEqual("RIFF"u8))
            {
                SkipChunk();
                continue;
            }

            return _readRiffChunk(reader);
        }
    }

    private static void _skipChunk(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        reader.BaseStream.Position += length;
    }

    private static void _readFourCC(BinaryReader reader, Span<byte> fourCc)
    {
        fourCc[0] = reader.ReadByte();
        fourCc[1] = reader.ReadByte();
        fourCc[2] = reader.ReadByte();
        fourCc[3] = reader.ReadByte();
    }

    private static WavData _readRiffChunk(BinaryReader reader)
    {
        Span<byte> format = stackalloc byte[4];
        reader.ReadUInt32();
        _readFourCC(reader, format);
        if (!format.SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException("File is not a WAVE file.");
        }

        _readFourCC(reader, format);
        if (!format.SequenceEqual("fmt "u8))
        {
            throw new InvalidDataException("Expected fmt chunk.");
        }

        // Read fmt chunk.

        var size = reader.ReadInt32();
        var afterFmtPos = reader.BaseStream.Position + size;

        var audioType = (WavAudioFormatType) reader.ReadInt16();
        var channels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        var byteRate = reader.ReadInt32();
        var blockAlign = reader.ReadInt16();
        var bitsPerSample = reader.ReadInt16();

        if (audioType != WavAudioFormatType.PCM)
        {
            throw new NotImplementedException("Unable to support audio types other than PCM.");
        }

        DebugTools.Assert(byteRate == sampleRate * channels * bitsPerSample / 8);

        // Fmt is not of guaranteed size, so use the size header to skip to the end.
        reader.BaseStream.Position = afterFmtPos;

        while (true)
        {
            _readFourCC(reader, format);
            if (!format.SequenceEqual("data"u8))
            {
                _skipChunk(reader);
                continue;
            }

            break;
        }

        // We are in the data chunk.
        size = reader.ReadInt32();
        var data = reader.ReadBytes(size);

        return new WavData(audioType, channels, sampleRate, byteRate, blockAlign, bitsPerSample, data);
    }

    /// <summary>
    ///     See http://soundfile.sapp.org/doc/WaveFormat/ for reference.
    /// </summary>
    internal readonly struct WavData
    {
        public readonly WavAudioFormatType AudioType;
        public readonly short NumChannels;
        public readonly int SampleRate;
        public readonly int ByteRate;
        public readonly short BlockAlign;
        public readonly short BitsPerSample;
        public readonly ReadOnlyMemory<byte> Data;

        public WavData(WavAudioFormatType audioType, short numChannels, int sampleRate, int byteRate,
            short blockAlign, short bitsPerSample, ReadOnlyMemory<byte> data)
        {
            AudioType = audioType;
            NumChannels = numChannels;
            SampleRate = sampleRate;
            ByteRate = byteRate;
            BlockAlign = blockAlign;
            BitsPerSample = bitsPerSample;
            Data = data;
        }
    }

    internal enum WavAudioFormatType : short
    {
        Unknown = 0,
        PCM = 1,
        // There's a bunch of other types, those are all unsupported.
    }

    #endregion
}
