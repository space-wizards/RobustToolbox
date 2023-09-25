using System;
using System.Numerics;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Sources;

[Virtual]
internal class BaseAudioSource : IAudioSource
{
    /*
     * This may look weird having all these methods here however
     * we need to handle disposing plus checking for errors hence we get this.
     */

    /// <summary>
    /// Handle to the AL source.
    /// </summary>
    private int SourceHandle;

    /// <summary>
    /// Source to the EFX filter if applicable.
    /// </summary>
    private int FilterHandle;

    private readonly AudioManager _master;

    /// <summary>
    /// Underlying stream to the audio.
    /// </summary>
    private readonly AudioStream _sourceStream;
#if DEBUG
    private bool _didPositionWarning;
#endif

    /// <summary>
    /// Prior gain that was set.
    /// </summary>
    private float _gain;

    private bool IsEfxSupported => _master.IsEfxSupported;

    public BaseAudioSource(AudioManager master, int sourceHandle, AudioStream sourceStream)
    {
        _master = master;
        SourceHandle = sourceHandle;
        _sourceStream = sourceStream;
        AL.GetSource(SourceHandle, ALSourcef.Gain, out _gain);
    }

    /// <inheritdoc />
    public virtual bool Playing
    {
        get
        {
            _checkDisposed();
            var state = AL.GetSourceState(SourceHandle);
            _master._checkAlError();
            return state == ALSourceState.Playing;
        }
        set
        {
            _checkDisposed();

            if (value)
            {
                AL.SourcePlay(SourceHandle);
            }
            else
            {
                AL.SourceStop(SourceHandle);
            }


            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public bool Looping
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

    /// <inheritdoc />
    public bool Global
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourceb.SourceRelative, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourceb.SourceRelative, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public Vector2 Position
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSource3f.Position, out var x, out var y, out _);
            _master._checkAlError();
            return new Vector2(x, y);
        }
        set
        {
            _checkDisposed();

            var (x, y) = value;

            if (!AreFinite(x, y))
            {
                return;
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
        }
    }

    /// <inheritdoc />
    public float Pitch { get; set; }

    /// <inheritdoc />
    public float Volume
    {
        get
        {
            // TODO: Sloth
            throw new NotImplementedException();
        }
        set => Gain = MathF.Pow(10, value / 10);
    }

    /// <inheritdoc />
    public float Gain
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.Gain, out var gain);
            _master._checkAlError();
            return gain;
        }
        set
        {
            _checkDisposed();
            var priorOcclusion = 1f;
            if (!IsEfxSupported)
            {
                AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
                priorOcclusion = priorGain / _gain;
            }

            _gain = value;
            AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float MaxDistance
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.MaxDistance, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.MaxDistance, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float RolloffFactor
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.RolloffFactor, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.RolloffFactor, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float ReferenceDistance
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.ReferenceDistance, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.ReferenceDistance, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float Occlusion
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.MaxDistance, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            var cutoff = MathF.Exp(-value * 1);
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
    }

    /// <inheritdoc />
    public float PlaybackPosition
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.SecOffset, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.SecOffset, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public Vector2 Velocity
    {
        get
        {
            _checkDisposed();

            AL.GetSource(SourceHandle, ALSource3f.Velocity, out var x, out var y, out _);
            _master._checkAlError();
            return new Vector2(x, y);
        }
        set
        {
            _checkDisposed();

            var (x, y) = value;

            if (!AreFinite(x, y))
            {
                return;
            }

            AL.Source(SourceHandle, ALSource3f.Velocity, x, y, 0);
            _master._checkAlError();
        }
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

    private static bool AreFinite(float x, float y)
    {
        if (float.IsFinite(x) && float.IsFinite(y))
        {
            return true;
        }

        return false;
    }

    ~BaseAudioSource()
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
            if (FilterHandle != 0)
                EFX.DeleteFilter(FilterHandle);

            AL.DeleteSource(SourceHandle);
            _master.RemoveAudioSource(SourceHandle);
            _master._checkAlError();
        }

        SourceHandle = -1;
    }

    protected bool _isDisposed()
    {
        return SourceHandle == -1;
    }

    protected void _checkDisposed()
    {
        if (SourceHandle == -1)
        {
            throw new ObjectDisposedException(nameof(BaseAudioSource));
        }
    }
}
