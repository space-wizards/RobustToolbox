using OpenTK.Audio.OpenAL;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.AudioLoading;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Robust.Client.Audio;

internal partial class AudioManager
{
    private float _zOffset;

    public float BaseGain { get; private set; }

    public float FadeGain { get; private set; } = 1f;

    #region Lifecycle

    /// <summary>Whether the audio backend initialized successfully and is actually producing sound.</summary>
    public bool IsInitialized => _audioInitialized;

    /// <summary>
    /// Finishes setting up the audio backend once a windowing/graphics context exists. Must be
    /// called from the game thread; captures it for later <see cref="IsMainThread"/> checks.
    /// </summary>
    public void InitializePostWindowing()
    {
        _gameThread = Thread.CurrentThread;

        OpenALSawmill = _logMan.GetSawmill("clyde.oal");
        PreloadOpenAl(OpenALSawmill);

        InitializeAudio();
    }

    /// <summary>Disposes all live audio sources/buffers and tears down the OpenAL context and device.</summary>
    public void Shutdown()
    {
        DisposeAllAudio();

        if (_openALContext != ALContext.Null)
        {
            ALC.MakeContextCurrent(ALContext.Null);

            ALC.DestroyContext(_openALContext);

            _openALContext = ALContext.Null;
        }

        if (_openALDevice != IntPtr.Zero)
        {
            ALC.CloseDevice(_openALDevice);

            _openALDevice = ALDevice.Null;
        }

        _cfg.UnsubValueChanged(CVars.AudioMasterVolume, SetMasterGain);
        _cfg.UnsubValueChanged(CVars.AudioMuteUnfocused, OnMuteUnfocusedChanged);
        _cfg.UnsubValueChanged(CVars.AudioDevice, OnAudioDeviceChanged);
        _clyde.OnWindowFocused -= OnWindowFocused;

        _reload.OnChanged -= OnReload;

        _audioInitialized = false;
    }

    public void FrameUpdate(float frameTime)
    {
        if (_hasPendingDeviceSwitch)
        {
            _hasPendingDeviceSwitch = false;
            SwitchAudioDevice(_pendingDeviceSwitch);
        }

        UpdateDeviceState(_gameTiming.RealTime);

        if (MathF.Abs(FadeGain - _masterFadeTargetGain) < 0.001f)
            return;

        _masterFadeElapsed = MathF.Min(_masterFadeElapsed + frameTime, MasterFadeDuration);
        var t = MasterFadeDuration <= 0f ? 1f : _masterFadeElapsed / MasterFadeDuration;
        FadeGain = MathHelper.Lerp(_masterFadeStartGain, _masterFadeTargetGain, t);
        ApplyMasterGain();
    }

    #endregion

    #region Listener State

    /// <summary>Sets the world-space Z offset applied to the listener position.</summary>
    public void SetZOffset(float offset) => _zOffset = offset;

    /// <inheritdoc/>
    public void SetVelocity(Vector2 velocity)
    {
        if (!_audioInitialized) return;
        AL.Listener(ALListener3f.Velocity, velocity.X, velocity.Y, 0f);
    }

    /// <inheritdoc/>
    public void SetPosition(Vector2 position)
    {
        if (!_audioInitialized) return;
        AL.Listener(ALListener3f.Position, position.X, position.Y, _zOffset);
    }

    /// <inheritdoc/>
    public void SetRotation(Angle angle)
    {
        if (!_audioInitialized) return;

        var az = (float)angle.Theta;
        var at = new OpenTK.Mathematics.Vector3(0f, 0f, 1f);
        var up = new OpenTK.Mathematics.Vector3(-MathF.Sin(az), MathF.Cos(az), 0f);
        AL.Listener(ALListenerfv.Orientation, ref at, ref up);
    }

    /// <summary>Sets the listener gain (master volume), clamped to be non-negative.</summary>
    public void SetMasterGain(float newGain)
    {
        if (newGain < 0f)
        {
            OpenALSawmill.Error("Tried to set master gain below 0, clamping to 0");
            newGain = 0f;
        }

        BaseGain = newGain;
        ApplyMasterGain();
    }

    /// <summary>Sets the active distance-attenuation model and, once initialized, applies it immediately.</summary>
    public void SetAttenuation(Attenuation attenuation)
    {
        _attenuation = attenuation;
        if (!_audioInitialized)
            return;
        ApplyDistanceModel();
    }

    public void SetDopplerFactor(float factor)
    {
        if (!_audioInitialized) return;

        factor = Math.Max(factor, 0f);
        AL.DopplerFactor(factor);
        OpenALSawmill.Info($"Set doppler factor to {factor:F2}");
    }

    /// <inheritdoc/>
    internal static float GetAttenuationGain(Attenuation attenuation, float distance, float rolloffFactor, float referenceDistance, float maxDistance)
    {
        // Mirrors the OpenAL 1.1 spec distance models (section 3.4.4) so callers can predict
        // the gain OpenAL will apply without querying a source.
        switch (attenuation)
        {
            case Attenuation.NoAttenuation:
                return 1f;

            case Attenuation.InverseDistance:
                // Unclamped inverse still needs a floor, otherwise the denominator can reach
                // zero and flip sign once distance drops far below the reference.
                distance = MathF.Max(distance, referenceDistance);
                return InverseGain(distance, rolloffFactor, referenceDistance);

            case Attenuation.InverseDistanceClamped:
                distance = MathF.Max(referenceDistance, MathF.Min(distance, maxDistance));
                return InverseGain(distance, rolloffFactor, referenceDistance);

            case Attenuation.LinearDistance:
                // Spec clamps to maxDistance here to avoid negative gain.
                distance = MathF.Min(distance, maxDistance);
                return LinearGain(distance, rolloffFactor, referenceDistance, maxDistance);

            case Attenuation.LinearDistanceClamped:
                distance = MathF.Max(referenceDistance, MathF.Min(distance, maxDistance));
                return LinearGain(distance, rolloffFactor, referenceDistance, maxDistance);

            case Attenuation.ExponentDistance:
                distance = MathF.Max(distance, referenceDistance);
                return ExponentGain(distance, rolloffFactor, referenceDistance);

            case Attenuation.ExponentDistanceClamped:
                distance = MathF.Max(referenceDistance, MathF.Min(distance, maxDistance));
                return ExponentGain(distance, rolloffFactor, referenceDistance);

            default:
                throw new ArgumentOutOfRangeException($"No attenuation formula for {attenuation}!");
        }
    }

    public float GetAttenuationGain(float distance, float rolloffFactor, float referenceDistance, float maxDistance)
        => GetAttenuationGain(_attenuation, distance, rolloffFactor, referenceDistance, maxDistance);

    private static float InverseGain(float distance, float rolloffFactor, float referenceDistance)
    {
        var denominator = referenceDistance + rolloffFactor * (distance - referenceDistance);
        return denominator <= 0f ? 1f : Math.Clamp(referenceDistance / denominator, 0f, 1f);
    }

    private static float LinearGain(float distance, float rolloffFactor, float referenceDistance, float maxDistance)
    {
        var range = maxDistance - referenceDistance;
        // Degenerate range: everything inside the reference is full volume, everything past it silent.
        if (range <= 0f)
            return distance <= referenceDistance ? 1f : 0f;

        return Math.Clamp(1f - rolloffFactor * (distance - referenceDistance) / range, 0f, 1f);
    }

    private static float ExponentGain(float distance, float rolloffFactor, float referenceDistance)
    {
        if (referenceDistance <= 0f || distance <= 0f)
            return 1f;

        return Math.Clamp(MathF.Pow(distance / referenceDistance, -rolloffFactor), 0f, 1f);
    }

    #endregion

    #region Audio Loading

    /// <inheritdoc/>
    public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
    {
        var vorbis = AudioLoaderOgg.LoadAudioData(stream);
        var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double)vorbis.SampleRate);

        if (!_audioInitialized)
            return new AudioStream(this, 0, new ClydeHandle(0), length, (int)vorbis.Channels, name, vorbis.Title, vorbis.Artist);

        var buffer = AL.GenBuffer();

        ALFormat format;
        // NVorbis only supports loading into floats.
        // If this becomes a problem due to missing extension support (doubt it but ok),
        // check the git history, I originally used libvorbisfile which worked and loaded 16 bit LPCM.
        if (vorbis.Channels == 1)
        {
            format = ALFormat.Mono16;
        }
        else if (vorbis.Channels == 2)
        {
            format = ALFormat.Stereo16;
        }
        else
        {
            throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
        }

        unsafe
        {
            fixed (short* ptr = vorbis.Data.Span)
            {
                AL.BufferData(buffer, format, (IntPtr)ptr, vorbis.Data.Length * sizeof(short),
                    (int)vorbis.SampleRate);
            }
        }

        CheckAlError();

        var handle = RegisterBuffer(buffer);
        return new AudioStream(this, buffer, handle, length, (int)vorbis.Channels, name, vorbis.Title, vorbis.Artist);
    }

    /// <inheritdoc/>
    public AudioStream LoadAudioWav(Stream stream, string? name = null)
    {
        var wav = AudioLoaderWav.LoadAudioData(stream);
        var length = TimeSpan.FromSeconds(wav.Data.Length / (double)wav.BlockAlign / wav.SampleRate);

        if (!_audioInitialized)
            return new AudioStream(this, 0, new ClydeHandle(0), length, wav.NumChannels, name);

        var buffer = AL.GenBuffer();

        ALFormat format;
        if (wav.BitsPerSample == 16)
        {
            if (wav.NumChannels == 1)
            {
                format = ALFormat.Mono16;
            }
            else if (wav.NumChannels == 2)
            {
                format = ALFormat.Stereo16;
            }
            else
            {
                throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
            }
        }
        else if (wav.BitsPerSample == 8)
        {
            if (wav.NumChannels == 1)
            {
                format = ALFormat.Mono8;
            }
            else if (wav.NumChannels == 2)
            {
                format = ALFormat.Stereo8;
            }
            else
            {
                throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
            }
        }
        else
        {
            throw new InvalidOperationException("Unable to load wav with bits per sample different from 8 or 16");
        }

        unsafe
        {
            fixed (byte* ptr = wav.Data.Span)
            {
                AL.BufferData(buffer, format, (IntPtr)ptr, wav.Data.Length, wav.SampleRate);
            }
        }

        CheckAlError();

        var handle = RegisterBuffer(buffer);
        return new AudioStream(this, buffer, handle, length, wav.NumChannels, name);
    }

    /// <inheritdoc/>
    public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
    {
        var length = TimeSpan.FromSeconds((double)samples.Length / channels / sampleRate);

        if (!_audioInitialized)
            return new AudioStream(this, 0, new ClydeHandle(0), length, channels, name);

        var fmt = channels switch
        {
            1 => ALFormat.Mono16,
            2 => ALFormat.Stereo16,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channels), "Only stereo and mono is currently supported")
        };

        var buffer = AL.GenBuffer();
        CheckAlError();

        unsafe
        {
            fixed (short* ptr = samples)
            {
                AL.BufferData(buffer, fmt, (IntPtr)ptr, samples.Length * sizeof(short), sampleRate);
            }
        }

        CheckAlError();

        var handle = RegisterBuffer(buffer);
        return new AudioStream(this, buffer, handle, length, channels, name);
    }

    /// <summary>Deletes the OpenAL buffer backing <paramref name="stream"/>, if it is still loaded.</summary>
    void IAudioInternal.Remove(AudioStream stream)
    {
        if (stream.ClydeHandle == null)
            return;

        if (!_audioSampleBuffers.Remove(stream.BufferId))
        {
            return;
        }

        AL.DeleteBuffer(stream.BufferId);
    }

    #endregion

    #region Source Creation

    /// <summary>Creates a one-shot AL source bound to the buffer of <paramref name="stream"/>.</summary>
    public IAudioSource? CreateAudioSource(AudioStream stream)
    {
        if (!_audioInitialized)
            return null;

        var source = AL.GenSource();

        if (!AL.IsSource(source))
        {
            OpenALSawmill.Error($"Failed to generate source. Too many simultaneous audio streams? {Environment.StackTrace}");
            return null;
        }

        if (!_audioSampleBuffers.TryGetValue(stream.BufferId, out var sample))
        {
            OpenALSawmill.Warning($"Audio stream '{stream.Name}' has no backing buffer, skipping.");
            AL.DeleteSource(source);
            return null;
        }

        AL.Source(source, ALSourcei.Buffer, sample.BufferHandle);

        var audioSource = new AudioSource(this, source, stream);
        _audioSources.Add(source, new WeakReference<BaseAudioSource>(audioSource));
        ApplyDefaultParams(audioSource);
        return audioSource;
    }

    /// <summary>Creates a streaming/buffered AL source with <paramref name="buffers"/> backing buffers.</summary>
    /// <inheritdoc/>
    IBufferedAudioSource? IAudioInternal.CreateBufferedAudioSource(int buffers, bool floatAudio)
    {
        if (!_audioInitialized)
            return null;

        var source = AL.GenSource();

        if (!AL.IsSource(source))
        {
            OpenALSawmill.Error($"Failed to generate source. Too many simultaneous audio streams? {Environment.StackTrace}");
            return null;
        }

        // ReSharper disable once PossibleInvalidOperationException

        var audioSource = new BufferedAudioSource(this, source, AL.GenBuffers(buffers), floatAudio);
        _bufferedAudioSources.Add(source, new WeakReference<BufferedAudioSource>(audioSource));
        ApplyDefaultParams(audioSource);
        return audioSource;
    }

    /// <summary>Applies the shared default <see cref="AudioParams"/> to a freshly created source.</summary>
    private void ApplyDefaultParams(IAudioSource source)
    {
        source.MaxDistance = AudioParams.Default.MaxDistance;
        source.Pitch = AudioParams.Default.Pitch;
        source.ReferenceDistance = AudioParams.Default.ReferenceDistance;
        source.RolloffFactor = AudioParams.Default.RolloffFactor;
    }

    /// <summary>Drops the bookkeeping entry for a disposed one-shot source. Does not touch AL state.</summary>
    internal void RemoveAudioSource(int handle) => _audioSources.Remove(handle);

    /// <summary>Drops the bookkeeping entry for a disposed buffered source. Does not touch AL state.</summary>
    internal void RemoveBufferedAudioSource(int handle) => _bufferedAudioSources.Remove(handle);

    #endregion

    #region Playback Control

    /// <inheritdoc />
    public void StopAllAudio()
    {
        if (!_audioInitialized) return;
        foreach (var source in _audioSources.Values)
            if (source.TryGetTarget(out var target))
                target.Playing = false;

        foreach (var source in _bufferedAudioSources.Values)
            if (source.TryGetTarget(out var target))
                target.Playing = false;
    }

    /// <summary>Disposes every live source and deletes every loaded buffer.</summary>
    public void DisposeAllAudio()
    {
        // Snapshot first: disposing a source calls back into RemoveAudioSource, which mutates
        // the very dictionary being enumerated.
        foreach (var source in _audioSources.Values.ToArray())
            if (source.TryGetTarget(out var target))
                target.Dispose();

        _audioSources.Clear();

        foreach (var source in _bufferedAudioSources.Values.ToArray())
            if (source.TryGetTarget(out var target))
                target.Dispose();

        _bufferedAudioSources.Clear();

        foreach (var buffer in _audioSampleBuffers.Values.ToArray())
            DeleteAudioBufferOnMainThread(buffer.BufferHandle);

        _audioSampleBuffers.Clear();
    }

    #endregion

    #region Device Management

    public IReadOnlyList<string> GetAudioDevices()
        => [.. EnumerateDevices().Where(d => !string.IsNullOrEmpty(d))];

    public string? GetDefaultAudioDevice()
    {
        if (ALC.EnumerateAll.IsExtensionPresent())
            return ALC.EnumerateAll.GetString(ALDevice.Null, GetEnumerateAllContextString.DefaultAllDevicesSpecifier);

        if (ALC.IsExtensionPresent(ALDevice.Null, "ALC_ENUMERATION_EXT"))
            return ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);

        return null;
    }

    public void UpdateDeviceState(TimeSpan curTime)
    {
        if (curTime < _nextDeviceCheck)
            return;

        _nextDeviceCheck = curTime + DeviceCheckInterval;

        if (!_audioInitialized)
        {
            TryLateInitialize();
            return;
        }

        if (IsDeviceConnected())
        {
            _reopenFailures = 0;
            return;
        }

        var preferred = GetPreferredDeviceName();

        if (TryReopenAudioDevice(preferred) || (preferred != null && TryReopenAudioDevice(null)))
        {
            _reopenFailures = 0;
            return;
        }

        // The device is gone and reopen can't recover it. Retrying forever just spams
        // the log and keeps issuing calls against a dead backend.
        if (++_reopenFailures < MaxReopenFailures)
            return;

        OpenALSawmill.Warning($"Reopen failed {_reopenFailures} times, rebuilding the audio device.");
        _reopenFailures = 0;
        RebuildAudioDevice();
    }

    public bool HasAlDeviceExtension(string extension) => _alcDeviceExtensions.Contains(extension);

    public bool HasAlContextExtension(string extension) => _alContextExtensions.Contains(extension);

    public string GetCurrentDeviceName()
    {
        if (ALC.EnumerateAll.IsExtensionPresent())
        {
            var name = ALC.EnumerateAll.GetString(_openALDevice, GetEnumerateAllContextString.AllDevicesSpecifier);
            if (!string.IsNullOrEmpty(name))
                return name;
        }

        return ALC.GetString(_openALDevice, AlcGetString.DeviceSpecifier) ?? "";
    }

    #endregion
}
