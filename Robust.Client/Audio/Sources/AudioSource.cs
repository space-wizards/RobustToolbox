using System;
using System.Numerics;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Sources;

internal sealed class AudioSource : IAudioSource
{
    private int SourceHandle;
    private readonly AudioManager _master;
    private readonly AudioStream _sourceStream;
    private int FilterHandle;
#if DEBUG
    private bool _didPositionWarning;
#endif

    private float _gain;

    private bool IsEfxSupported => _master.IsEfxSupported;

    public AudioSource(AudioManager master, int sourceHandle, AudioStream sourceStream)
    {
        _master = master;
        SourceHandle = sourceHandle;
        _sourceStream = sourceStream;
        AL.GetSource(SourceHandle, ALSourcef.Gain, out _gain);
    }

    public void StartPlaying()
    {
        _checkDisposed();
        AL.SourcePlay(SourceHandle);
        _master._checkAlError();
    }

    public void StopPlaying()
    {
        if (_isDisposed()) return;
        AL.SourceStop(SourceHandle);
        _master._checkAlError();
    }

    public bool IsPlaying
    {
        get
        {
            _checkDisposed();
            var state = AL.GetSourceState(SourceHandle);
            return state == ALSourceState.Playing;
        }
    }

    public bool IsLooping
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourceb.Looping, out var ret);
            _master._checkAlError();
            return ret;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourceb.Looping, value);
            _master._checkAlError();
        }
    }

    public bool IsGlobal
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourceb.SourceRelative, out var value);
            _master._checkAlError();
            return value;
        }
    }

    public void SetGlobal()
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourceb.SourceRelative, true);
        _master._checkAlError();
    }

    public void SetVolume(float decibels)
    {
        _checkDisposed();
        var priorOcclusion = 1f;
        if (!IsEfxSupported)
        {
            AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
            priorOcclusion = priorGain / _gain;
        }
        _gain =  MathF.Pow(10, decibels / 10);
        AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
        _master._checkAlError();
    }

    public void SetVolumeDirect(float gain)
    {
        _checkDisposed();
        var priorOcclusion = 1f;
        if (!IsEfxSupported)
        {
            AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
            priorOcclusion = priorGain / _gain;
        }
        _gain = gain;
        AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
        _master._checkAlError();
    }

    public void SetMaxDistance(float distance)
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourcef.MaxDistance, distance);
        _master._checkAlError();
    }

    public void SetRolloffFactor(float rolloffFactor)
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourcef.RolloffFactor, rolloffFactor);
        _master._checkAlError();
    }

    public void SetReferenceDistance(float refDistance)
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourcef.ReferenceDistance, refDistance);
        _master._checkAlError();
    }

    public void SetOcclusion(float blocks)
    {
        _checkDisposed();
        var cutoff = MathF.Exp(-blocks * 1);
        var gain = MathF.Pow(cutoff, 0.1f);
        if (IsEfxSupported)
        {
            SetOcclusionEfx(gain, cutoff);
        }
        else
        {
            gain *= gain * gain;
            AL.Source(SourceHandle, ALSourcef.Gain, _gain * gain);
        }
        _master._checkAlError();
    }

    private void SetOcclusionEfx(float gain, float cutoff)
    {
        if (FilterHandle == 0)
        {
            FilterHandle = EFX.GenFilter();
            EFX.Filter(FilterHandle, FilterInteger.FilterType, (int) FilterType.Lowpass);
        }

        EFX.Filter(FilterHandle, FilterFloat.LowpassGain, gain);
        EFX.Filter(FilterHandle, FilterFloat.LowpassGainHF, cutoff);
        AL.Source(SourceHandle, ALSourcei.EfxDirectFilter, FilterHandle);
    }

    public void SetPlaybackPosition(float seconds)
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourcef.SecOffset, seconds);
        _master._checkAlError();
    }

    public bool SetPosition(Vector2 position)
    {
        _checkDisposed();

        var (x, y) = position;

        if (!AreFinite(x, y))
        {
            return false;
        }
#if DEBUG
        // OpenAL doesn't seem to want to play stereo positionally.
        // Log a warning if people try to.
        if (_sourceStream.ChannelCount > 1 && !_didPositionWarning)
        {
            _didPositionWarning = true;
            _master.OpenALSawmill.Warning("Attempting to set position on audio source with multiple audio channels! Stream: '{0}'.  Make sure the audio is MONO, not stereo.",
                _sourceStream.Name);
            // warning isn't enough, people just ignore it :(
            DebugTools.Assert(false, $"Attempting to set position on audio source with multiple audio channels! Stream: '{_sourceStream.Name}'. Make sure the audio is MONO, not stereo.");
        }
#endif

        AL.Source(SourceHandle, ALSource3f.Position, x, y, 0);
        _master._checkAlError();
        return true;
    }

    private static bool AreFinite(float x, float y)
    {
        if (float.IsFinite(x) && float.IsFinite(y))
        {
            return true;
        }

        return false;
    }

    public void SetVelocity(Vector2 velocity)
    {
        _checkDisposed();

        var (x, y) = velocity;

        if (!AreFinite(x, y))
        {
            return;
        }

        AL.Source(SourceHandle, ALSource3f.Velocity, x, y, 0);

        _master._checkAlError();
    }

    public void SetPitch(float pitch)
    {
        _checkDisposed();
        AL.Source(SourceHandle, ALSourcef.Pitch, pitch);
        _master._checkAlError();
    }

    ~AudioSource()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            // We can't run this code inside the finalizer thread so tell Clyde to clear it up later.
            _master.DeleteSourceOnMainThread(SourceHandle, FilterHandle);
        }
        else
        {
            if (FilterHandle != 0) EFX.DeleteFilter(FilterHandle);
            AL.DeleteSource(SourceHandle);
            _master._audioSources.Remove(SourceHandle);
            _master._checkAlError();
        }

        SourceHandle = -1;
    }

    private bool _isDisposed()
    {
        return SourceHandle == -1;
    }

    private void _checkDisposed()
    {
        if (SourceHandle == -1)
        {
            throw new ObjectDisposedException(nameof(AudioSource));
        }
    }
}