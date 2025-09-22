using System;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio.AudioLoading;

/// <summary>
/// Implements functionality for loading wav audio files.
/// </summary>
/// <seealso cref="AudioLoaderOgg"/>
internal static class AudioLoaderWav
{
    /// <summary>
    /// Load metadata for a wav audio file.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static AudioMetadata LoadAudioMetadata(Stream stream)
    {
        // TODO: Don't load entire WAV file just to extract metadata.
        var data = LoadAudioData(stream);
        var length = TimeSpan.FromSeconds(data.Data.Length / (double) data.BlockAlign / data.SampleRate);
        return new AudioMetadata(length, data.NumChannels);
    }

    /// <summary>
    /// Load a wav file into raw samples and metadata.
    /// </summary>
    /// <param name="stream">Audio file stream to load.</param>
    public static WavData LoadAudioData(Stream stream)
    {
        var reader = new BinaryReader(stream, EncodingHelpers.UTF8, true);

        // Read outer most chunks.
        Span<byte> fourCc = stackalloc byte[4];
        while (true)
        {
            ReadFourCC(reader, fourCc);

            if (!fourCc.SequenceEqual("RIFF"u8))
            {
                SkipChunk(reader);
                continue;
            }

            return ReadRiffChunk(reader);
        }
    }

    private static void SkipChunk(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        reader.BaseStream.Position += length;
    }

    private static void ReadFourCC(BinaryReader reader, Span<byte> fourCc)
    {
        fourCc[0] = reader.ReadByte();
        fourCc[1] = reader.ReadByte();
        fourCc[2] = reader.ReadByte();
        fourCc[3] = reader.ReadByte();
    }

    private static WavData ReadRiffChunk(BinaryReader reader)
    {
        Span<byte> format = stackalloc byte[4];
        reader.ReadUInt32();
        ReadFourCC(reader, format);
        if (!format.SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException("File is not a WAVE file.");
        }

        ReadFourCC(reader, format);
        if (!format.SequenceEqual("fmt "u8))
        {
            throw new InvalidDataException("Expected fmt chunk.");
        }

        // Read fmt chunk.

        var size = reader.ReadInt32();
        var afterFmtPos = reader.BaseStream.Position + size;

        var audioType = (WavAudioFormatType)reader.ReadInt16();
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
            ReadFourCC(reader, format);
            if (!format.SequenceEqual("data"u8))
            {
                SkipChunk(reader);
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
}
