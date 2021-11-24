using System;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Audio
{
    internal partial class ClydeAudio
    {
        /// <summary>
        ///     Load up a WAVE file.
        /// </summary>
        private static WavData _readWav(Stream stream)
        {
            var reader = new BinaryReader(stream, EncodingHelpers.UTF8, true);

            void SkipChunk()
            {
                var length = reader.ReadUInt32();
                stream.Position += length;
            }

            // Read outer most chunks.
            Span<char> fourCc = stackalloc char[4];
            while (true)
            {
                _readFourCC(reader, fourCc);

                if (!fourCc.SequenceEqual("RIFF"))
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

        private static void _readFourCC(BinaryReader reader, Span<char> fourCc)
        {
            fourCc[0] = (char) reader.ReadByte();
            fourCc[1] = (char) reader.ReadByte();
            fourCc[2] = (char) reader.ReadByte();
            fourCc[3] = (char) reader.ReadByte();
        }

        private static WavData _readRiffChunk(BinaryReader reader)
        {
            Span<char> format = stackalloc char[4];
            reader.ReadUInt32();
            _readFourCC(reader, format);
            if (!format.SequenceEqual("WAVE"))
            {
                throw new InvalidDataException("File is not a WAVE file.");
            }

            _readFourCC(reader, format);
            if (!format.SequenceEqual("fmt "))
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
                if (!format.SequenceEqual("data"))
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
        [PublicAPI]
        private readonly struct WavData
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

        private enum WavAudioFormatType : short
        {
            Unknown = 0,
            PCM = 1,
            // There's a bunch of other types, those are all unsupported.
        }
    }
}
