using System;
using System.IO;
using System.Numerics;
using System.Threading;
using OpenTK.Audio.OpenAL;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Maths;

namespace Robust.Client.Audio;

internal partial class AudioManager
{
    public void InitializePostWindowing()
    {
        _gameThread = Thread.CurrentThread;
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
    public void SetPosition(Vector2 position)
    {
        AL.Listener(ALListener3f.Position, position.X, position.Y, -5);
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

    /// <inheritdoc/>
    public override AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
    {
        var vorbis = _readOggVorbis(stream);

        var buffer = AL.GenBuffer();

        ALFormat format;
        // NVorbis only supports loading into floats.
        // If this becomes a problem due to missing extension support (doubt it but ok),
        // check the git history, I originally used libvorbisfile which worked and loaded 16 bit LPCM.
        if (vorbis.Channels == 1)
        {
            format = ALFormat.MonoFloat32Ext;
        }
        else if (vorbis.Channels == 2)
        {
            format = ALFormat.StereoFloat32Ext;
        }
        else
        {
            throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
        }

        unsafe
        {
            fixed (float* ptr = vorbis.Data.Span)
            {
                AL.BufferData(buffer, format, (IntPtr) ptr, vorbis.Data.Length * sizeof(float),
                    (int) vorbis.SampleRate);
            }
        }

        _checkAlError();

        var handle = new ClydeHandle(_audioSampleBuffers.Count);
        _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
        var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double) vorbis.SampleRate);
        return new AudioStream(handle, length, (int) vorbis.Channels, name, vorbis.Title, vorbis.Artist);
    }

    /// <inheritdoc/>
    public override AudioStream LoadAudioWav(Stream stream, string? name = null)
    {
        var wav = _readWav(stream);

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
        _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
        var length = TimeSpan.FromSeconds(wav.Data.Length / (double) wav.BlockAlign / wav.SampleRate);
        return new AudioStream(handle, length, wav.NumChannels, name);
    }

    /// <inheritdoc/>
    public override AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
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
        _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
        return new AudioStream(handle, length, channels, name);
    }

    public void SetMasterVolume(float newVolume)
    {
        AL.Listener(ALListenerf.Gain, BaseGain * newVolume);
    }

    public void SetAudioAttenuation(int value)
    {
        var attenuation = (Attenuation) value;

        switch (attenuation)
        {
            case Attenuation.NoAttenuation:
                AL.DistanceModel(ALDistanceModel.None);
                break;
            case Attenuation.InverseDistance:
                AL.DistanceModel(ALDistanceModel.InverseDistance);
                break;
            case Attenuation.Default:
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

        var attToString = attenuation == Attenuation.Default ? Attenuation.InverseDistanceClamped : attenuation;

        OpenALSawmill.Info($"Set audio attenuation to {attToString.ToString()}");
    }

    public IAudioSource? CreateAudioSource(AudioResource resource)
    {
        var source = AL.GenSource();

        if (!AL.IsSource(source))
        {
            OpenALSawmill.Error("Failed to generate source. Too many simultaneous audio streams? {0}", Environment.StackTrace);
            return null;
        }

        // ReSharper disable once PossibleInvalidOperationException
        // TODO: This really shouldn't be indexing based on the ClydeHandle...
        AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[(int) resource.AudioStream.ClydeHandle!.Value].BufferHandle);

        var audioSource = new AudioSource(this, source, resource.AudioStream);
        _audioSources.Add(source, new WeakReference<AudioSource>(audioSource));
        return audioSource;
    }

    public IBufferedAudioSource? CreateBufferedAudioSource(int buffers, bool floatAudio=false)
    {
        var source = AL.GenSource();

        if (!AL.IsSource(source))
            throw new Exception("Failed to generate source. Too many simultaneous audio streams?");

        // ReSharper disable once PossibleInvalidOperationException

        var audioSource = new BufferedAudioSource(this, source, AL.GenBuffers(buffers), floatAudio);
        _bufferedAudioSources.Add(source, new WeakReference<BufferedAudioSource>(audioSource));
        return audioSource;
    }

    public void StopAllAudio()
    {
        foreach (var source in _audioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.StopPlaying();
            }
        }

        foreach (var source in _bufferedAudioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.StopPlaying();
            }
        }
    }

    public void DisposeAllAudio()
    {
        foreach (var source in _audioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.StopPlaying();
                target.Dispose();
            }
        }
        _audioSources.Clear();

        foreach (var source in _bufferedAudioSources.Values)
        {
            if (source.TryGetTarget(out var target))
            {
                target.StopPlaying();
                target.Dispose();
            }
        }
        _bufferedAudioSources.Clear();
    }
}
