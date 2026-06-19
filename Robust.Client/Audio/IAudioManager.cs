using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Audio.Sources;

namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
[NotContentImplementable]
public interface IAudioManager
{
    IAudioSource? CreateAudioSource(AudioStream stream);

    /// <summary>
    /// Creates a streaming audio source that PCM frames can be queued into at runtime.
    /// Used for real-time audio such as voice chat.
    /// </summary>
    /// <returns>null if unable to create the source.</returns>
    IBufferedAudioSource? CreateBufferedAudioSource(int buffers, bool floatAudio = false);

    AudioStream LoadAudioOggVorbis(Stream stream, string? name = null);

    AudioStream LoadAudioWav(Stream stream, string? name = null);

    AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null);

    void SetMasterGain(float gain);

    /// <summary>
    /// Lists the names of available microphone (capture) devices. The names can be passed to
    /// <see cref="OpenAudioInput"/>.
    /// </summary>
    IReadOnlyList<string> GetAudioInputDevices();

    /// <summary>
    /// Lists the names of available audio output (playback) devices. A name can be set on the
    /// <c>audio.device</c> CVar to select the output device used for all game audio (applied on next launch).
    /// </summary>
    IReadOnlyList<string> GetAudioOutputDevices();

    /// <summary>
    /// Opens a microphone for recording mono 16-bit PCM.
    /// </summary>
    /// <param name="deviceName">Capture device name, or null/empty for the system default.</param>
    /// <param name="sampleRate">Desired sample rate in Hz.</param>
    /// <param name="internalBufferSamples">
    /// Size of the device's internal ring buffer in samples. 0 picks a sensible default (~1 second).
    /// </param>
    /// <returns>The opened device, or null on failure. Caller must dispose it.</returns>
    IAudioInputDevice? OpenAudioInput(string? deviceName, int sampleRate, int internalBufferSamples = 0);

    /// <summary>
    /// Creates an Opus voice encoder for the given format. Caller must dispose it.
    /// </summary>
    IOpusEncoder CreateOpusEncoder(int sampleRate, int channels);

    /// <summary>
    /// Creates an Opus voice decoder for the given format. Caller must dispose it.
    /// </summary>
    IOpusDecoder CreateOpusDecoder(int sampleRate, int channels);

    /// <summary>
    /// Creates an offline speech-to-text transcriber from a local model file. Caller must dispose it.
    /// </summary>
    /// <param name="modelPath">Path to a Whisper ggml model file.</param>
    /// <param name="language">Language code to transcribe (e.g. "en"), or "auto" to auto-detect.</param>
    /// <returns>The transcriber, or null if the model could not be loaded.</returns>
    ISpeechTranscriber? CreateSpeechTranscriber(string modelPath, string language = "auto");
}
