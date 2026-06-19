using System;

namespace Robust.Client.Audio;

/// <summary>
/// <see cref="IOpusEncoder"/> backed by the Concentus pure-managed Opus implementation.
/// Concentus types are fully qualified to avoid colliding with the engine's own <see cref="IOpusEncoder"/>.
/// </summary>
internal sealed class OpusEncoderWrapper : IOpusEncoder
{
    private readonly Concentus.IOpusEncoder _encoder;

    public OpusEncoderWrapper(int sampleRate, int channels)
    {
        _encoder = Concentus.OpusCodecFactory.CreateEncoder(
            sampleRate, channels, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP, null);
    }

    public int Encode(ReadOnlySpan<short> pcm, int frameSize, Span<byte> output)
    {
        return _encoder.Encode(pcm, frameSize, output, output.Length);
    }

    public void Dispose()
    {
        // Concentus codecs are pure-managed; nothing unmanaged to release.
    }
}

/// <summary>
/// <see cref="IOpusDecoder"/> backed by the Concentus pure-managed Opus implementation.
/// </summary>
internal sealed class OpusDecoderWrapper : IOpusDecoder
{
    private readonly Concentus.IOpusDecoder _decoder;

    public OpusDecoderWrapper(int sampleRate, int channels)
    {
        _decoder = Concentus.OpusCodecFactory.CreateDecoder(sampleRate, channels, null);
    }

    public int Decode(ReadOnlySpan<byte> data, Span<short> output, int frameSize, bool decodeFec)
    {
        return _decoder.Decode(data, output, frameSize, decodeFec);
    }

    public void Dispose()
    {
    }
}
