using System;
using System.IO;
using System.Numerics;
using System.Threading;
using OpenTK.Audio.OpenAL;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.AudioLoading;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;

namespace Robust.Client.Audio;

internal partial class AudioManager
{
    private float _zOffset;

    public void SetZOffset(float offset)
    {
        _zOffset = offset;
    }

    /// <inheritdoc />
    public float GetAttenuationGain(float distance, float rolloffFactor, float referenceDistance, float maxDistance)
    {
        switch (_attenuation)
        {
            case Attenuation.LinearDistance:
                return 1 - rolloffFactor * (distance - referenceDistance) / (maxDistance - referenceDistance);
            case Attenuation.LinearDistanceClamped:
                distance = MathF.Max(referenceDistance, MathF.Min(distance, maxDistance));
                return 1 - rolloffFactor * (distance - referenceDistance) / (maxDistance - referenceDistance);
            default:
                // TODO: If you see this you can implement
                throw new NotImplementedException();
        }
    }

    public void InitializePostWindowing()
    {
        _gameThread = Thread.CurrentThread;
        InitializeAudio();
    }

    public void Shutdown()
    {
        DisposeAllAudio();

        if (_openALContext != ALContext.Null)
        {
            ALC.MakeContextCurrent(ALContext.Null);

            ALC.DestroyContext(_openALContext);
        }

        if (_openALDevice != IntPtr.Zero)
        {
            ALC.CloseDevice(_openALDevice);
        }
    }

    /// <inheritdoc/>
    public void SetVelocity(Vector2 velocity)
    {
        AL.Listener(ALListener3f.Velocity, velocity.X, velocity.Y, 0f);
    }

    /// <inheritdoc/>
    public void SetPosition(Vector2 position)
    {
        AL.Listener(ALListener3f.Position, position.X, position.Y, _zOffset);
    }

    /// <inheritdoc/>
    public void SetRotation(Angle angle)
    {
        var vec = angle.ToVec();

        // Default orientation: at: (0, 0, -1)  up: (0, 1, 0)
        var at = new OpenTK.Mathematics.Vector3(0f, 0f, -1f);
        var up = new OpenTK.Mathematics.Vector3(vec.Y, vec.X, 0f);
        AL.Listener(ALListenerfv.Orientation, new []{0, 0, -1, vec.X, vec.Y, 0});
        AL.Listener(ALListenerfv.Orientation, ref at, ref up);
    }

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

    /// <inheritdoc/>
    public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
    {
        var vorbis = AudioLoaderOgg.LoadAudioData(stream);

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
                AL.BufferData(buffer, format, (IntPtr) ptr, vorbis.Data.Length * sizeof(short),
                    (int) vorbis.SampleRate);
            }
        }

        _checkAlError();

        var handle = new ClydeHandle(_audioSampleBuffers.Count);
        _audioSampleBuffers.Add(buffer, new LoadedAudioSample(buffer));
        var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double) vorbis.SampleRate);
        return new AudioStream(this, buffer, handle, length, (int) vorbis.Channels, name, vorbis.Title, vorbis.Artist);
    }

    /// <inheritdoc/>
    public AudioStream LoadAudioWav(Stream stream, string? name = null)
    {
        var wav = AudioLoaderWav.LoadAudioData(stream);

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
                AL.BufferData(buffer, format, (IntPtr) ptr, wav.Data.Length, wav.SampleRate);
            }
        }

        _checkAlError();

        var handle = new ClydeHandle(_audioSampleBuffers.Count);
        _audioSampleBuffers.Add(buffer, new LoadedAudioSample(buffer));
        var length = TimeSpan.FromSeconds(wav.Data.Length / (double) wav.BlockAlign / wav.SampleRate);
        return new AudioStream(this, buffer, handle, length, wav.NumChannels, name);
    }

    /// <inheritdoc/>
    public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
    {
        var fmt = channels switch
        {
            1 => ALFormat.Mono16,
            2 => ALFormat.Stereo16,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channels), "Only stereo and mono is currently supported")
        };

        var buffer = AL.GenBuffer();
        _checkAlError();

        unsafe
        {
            fixed (short* ptr = samples)
            {
                AL.BufferData(buffer, fmt, (IntPtr) ptr, samples.Length * sizeof(short), sampleRate);
            }
        }

        _checkAlError();

        var handle = new ClydeHandle(_audioSampleBuffers.Count);
        var length = TimeSpan.FromSeconds((double) samples.Length / channels / sampleRate);
        _audioSampleBuffers.Add(buffer, new LoadedAudioSample(buffer));
        return new AudioStream(this, buffer, handle, length, channels, name);
    }

    public void SetMasterGain(float newGain)
    {
        if (newGain < 0f)
        {
            OpenALSawmill.Error("Tried to set master gain below 0, clamping to 0");
            AL.Listener(ALListenerf.Gain, 0f);
            return;
        }


        #region Platform hack for MacOS
        // HACK/BUG: Apple's OpenAL implementation has a bug where values of 0f for listener gain don't actually
        // HACK/BUG: prevent sound playback. Workaround is to cap the minimum gain at a value just above 0.
        if (OperatingSystem.IsMacOS() && newGain == 0f)
        {
            OpenALSawmill.Verbose("Not setting gain to 0 because Apple can't write an OpenAL implementation");
            AL.Listener(ALListenerf.Gain, float.Epsilon);
            return;
        }
        #endregion Platform hack for MacOS

        AL.Listener(ALListenerf.Gain, newGain);
    }

    public void SetAttenuation(Attenuation attenuation)
    {
        switch (attenuation)
        {
            case Attenuation.NoAttenuation:
                AL.DistanceModel(ALDistanceModel.None);
                break;
            case Attenuation.InverseDistance:
                AL.DistanceModel(ALDistanceModel.InverseDistance);
                break;
            case Attenuation.InverseDistanceClamped:
                AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
                break;
            case Attenuation.LinearDistance:
                AL.DistanceModel(ALDistanceModel.LinearDistance);
                break;
            case Attenuation.LinearDistanceClamped:
                AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);
                break;
            case Attenuation.ExponentDistance:
                AL.DistanceModel(ALDistanceModel.ExponentDistance);
                break;
            case Attenuation.ExponentDistanceClamped:
                AL.DistanceModel(ALDistanceModel.ExponentDistanceClamped);
                break;
            default:
                throw new ArgumentOutOfRangeException($"No implementation to set {attenuation.ToString()} for DistanceModel!");
        }

        _attenuation = attenuation;
        OpenALSawmill.Info($"Set audio attenuation to {attenuation.ToString()}");
    }

    internal void RemoveAudioSource(int handle)
    {
        _audioSources.Remove(handle);
    }

    internal void RemoveBufferedAudioSource(int handle)
    {
        _bufferedAudioSources.Remove(handle);
    }

    public IAudioSource? CreateAudioSource(AudioStream stream)
    {
        var source = AL.GenSource();

        if (!AL.IsSource(source))
        {
            OpenALSawmill.Error("Failed to generate source. Too many simultaneous audio streams? {0}", Environment.StackTrace);
            return null;
        }

        // ReSharper disable once PossibleInvalidOperationException
        // TODO: This really shouldn't be indexing based on the ClydeHandle...
        AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[stream.BufferId].BufferHandle);

        var audioSource = new AudioSource(this, source, stream);
        _audioSources.Add(source, new WeakReference<BaseAudioSource>(audioSource));
        ApplyDefaultParams(audioSource);
        return audioSource;
    }

    /// <inheritdoc/>
    IBufferedAudioSource? IAudioInternal.CreateBufferedAudioSource(int buffers, bool floatAudio)
    {
        var source = AL.GenSource();

        if (!AL.IsSource(source))
        {
            OpenALSawmill.Error("Failed to generate source. Too many simultaneous audio streams? {0}", Environment.StackTrace);
            return null;
        }

        // ReSharper disable once PossibleInvalidOperationException

        var audioSource = new BufferedAudioSource(this, source, AL.GenBuffers(buffers), floatAudio);
        _bufferedAudioSources.Add(source, new WeakReference<BufferedAudioSource>(audioSource));
        ApplyDefaultParams(audioSource);
        return audioSource;
    }

    private void ApplyDefaultParams(IAudioSource source)
    {
        source.MaxDistance = AudioParams.Default.MaxDistance;
        source.Pitch = AudioParams.Default.Pitch;
        source.ReferenceDistance = AudioParams.Default.ReferenceDistance;
        source.RolloffFactor = AudioParams.Default.RolloffFactor;
    }

    /// <inheritdoc />
    public void StopAllAudio()
    {
        foreach (var source in _audioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.Playing = false;
            }
        }

        foreach (var source in _bufferedAudioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.Playing = false;
            }
        }
    }

    public void DisposeAllAudio()
    {
        // TODO: Do we even need to stop?
        foreach (var source in _audioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.Dispose();
            }
        }

        _audioSources.Clear();

        foreach (var source in _bufferedAudioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.Dispose();
            }
        }

        _bufferedAudioSources.Clear();

        foreach (var buffer in _audioSampleBuffers.Values)
        {
            DeleteAudioBufferOnMainThread(buffer.BufferHandle);
        }

        _audioSampleBuffers.Clear();
    }
}
