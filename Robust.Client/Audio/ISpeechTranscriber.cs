using System;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Client.Audio;

/// <summary>
/// Offline speech-to-text transcriber backed by a local model.
/// </summary>
/// <remarks>
/// This lives in the engine because content assemblies are sandboxed and cannot reference the native
/// transcription library directly. Content gets one of these via <see cref="IAudioManager.CreateSpeechTranscriber"/>,
/// e.g. to transcribe captured voice-chat audio.
/// </remarks>
public interface ISpeechTranscriber : IDisposable
{
    /// <summary>
    /// Transcribes a buffer of mono 16-bit PCM, sampled at 16 kHz, into text.
    /// </summary>
    /// <param name="pcm">Mono 16-bit PCM samples at 16 kHz. Other sample rates are not supported by the model.</param>
    /// <param name="cancel">Cancels the in-progress transcription.</param>
    /// <returns>
    /// The recognised text (all segments concatenated), or an empty string if nothing was recognised.
    /// </returns>
    Task<string> TranscribeAsync(ReadOnlyMemory<short> pcm, CancellationToken cancel = default);
}
