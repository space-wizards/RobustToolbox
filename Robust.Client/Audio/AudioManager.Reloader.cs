using System;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using Robust.Shared.Graphics;

namespace Robust.Client.Audio;

internal sealed partial class AudioManager
{
    private readonly Dictionary<IClydeHandle, AudioVorbisCast> _audioVorbisCasts = new();
    private readonly Dictionary<IClydeHandle, LoadedAudioSample> _loadedAudioSamples = new();

    public void NotifySourceDisposed(AudioStream sourceStream)
    {
        OpenALSawmill.Debug($"Ended a source life with a {sourceStream.ClydeHandle} handle.");
        var audioSample = _loadedAudioSamples[sourceStream.ClydeHandle!];
        audioSample.DecreaseUsings();

        if (!audioSample.IsSafeToDelete())
            return;

        OpenALSawmill.Debug($"Enqueued {sourceStream.ClydeHandle} handle's buffer to free up.");
        _loadedAudioSamples.Remove(sourceStream.ClydeHandle!);
        _bufferDisposeQueue.Enqueue(audioSample.BufferHandle);
    }

    internal AudioVorbisCast MakeVorbisCast(IClydeHandle handle, ALFormat format, short[] buffer, int size, int sampleRate)
    {
        var vorbisCast = new AudioVorbisCast(format, buffer, size, sampleRate);
        _audioVorbisCasts.Add(handle, vorbisCast);
        return vorbisCast;
    }

    internal LoadedAudioSample? EnsureAudioSample(IClydeHandle handle)
    {
        if (_loadedAudioSamples.TryGetValue(handle, out var value))
        {
            return value;
        }

        var buffer = AL.GenBuffer();

        _checkAlError();

        if (!_audioVorbisCasts.TryGetValue(handle, out var vorbis))
        {
            OpenALSawmill.Error($"Could not find audio cast for {handle}.");
            return null;
        }

        unsafe
        {
            fixed (short* prt = vorbis.Buffer)
            {
                AL.BufferData(buffer, vorbis.Format, (IntPtr) prt, vorbis.Size, vorbis.SampleRate);
            }
        }

        _checkAlError();

        var sample = new LoadedAudioSample(buffer);
        _loadedAudioSamples.Add(handle, sample);

        return sample;
    }

    internal readonly struct AudioVorbisCast
    {
        public readonly ALFormat Format;
        public readonly short[] Buffer;
        public readonly int Size;
        public readonly int SampleRate;

        public AudioVorbisCast(ALFormat format, short[] buffer, int size, int sampleRate)
        {
            Format = format;
            Buffer = buffer;
            Size = size;
            SampleRate = sampleRate;
        }
    }
}
