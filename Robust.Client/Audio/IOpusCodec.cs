using System;

namespace Robust.Client.Audio;

/// <summary>
/// Opus voice encoder. Encodes 16-bit PCM frames into compressed Opus packets.
/// </summary>
/// <remarks>
/// This lives in the engine because content assemblies are sandboxed and cannot reference the Concentus
/// codec library directly. Content gets one of these via <see cref="IAudioManager.CreateOpusEncoder"/>.
/// </remarks>
public interface IOpusEncoder : IDisposable
{
    /// <summary>
    /// Encodes a single frame of PCM (<paramref name="frameSize"/> samples per channel) into <paramref name="output"/>.
    /// </summary>
    /// <returns>The number of bytes written to <paramref name="output"/>, or a non-positive value on failure.</returns>
    int Encode(ReadOnlySpan<short> pcm, int frameSize, Span<byte> output);
}

/// <summary>
/// Opus voice decoder. See <see cref="IOpusEncoder"/> for why this lives in the engine.
/// </summary>
public interface IOpusDecoder : IDisposable
{
    /// <summary>
    /// Decodes a single Opus packet into <paramref name="output"/>.
    /// </summary>
    /// <returns>The number of samples (per channel) decoded.</returns>
    int Decode(ReadOnlySpan<byte> data, Span<short> output, int frameSize, bool decodeFec);
}
