using System;
using System.Numerics;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Client.Audio.Effects;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;

namespace Robust.Client.Audio.Sources;

public abstract class BaseAudioSource : IAudioSource
{
    /*
     * This may look weird having all these methods here however
     * we need to handle disposing plus checking for errors hence we get this.
     */

    /// <summary>
    /// Handle to the AL source.
    /// </summary>
    protected int SourceHandle;

    /// <summary>
    /// Source to the EFX filter if applicable.
    /// </summary>
    protected int FilterHandle;

    internal readonly AudioManager Master;

    /// <summary>
    /// Prior gain that was set.
    /// </summary>
    private float _gain;

    private float _occlusion;

    private bool IsEfxSupported => Master.IsEfxSupported;

    internal BaseAudioSource(AudioManager master, int sourceHandle)
    {
        Master = master;
        SourceHandle = sourceHandle;
        AL.GetSource(SourceHandle, ALSourcef.Gain, out _gain);
    }

    /// <inheritdoc />
    public void Pause()
    {
        AL.SourcePause(SourceHandle);
    }

    /// <inheritdoc />
    public void StartPlaying()
    {
        if (Playing)
            return;

        Playing = true;
    }

    /// <inheritdoc />
    public void StopPlaying()
    {
        if (!Playing)
            return;

        Playing = false;
    }

    /// <inheritdoc />
    public void Restart()
    {
        AL.SourceRewind(SourceHandle);
        StartPlaying();
    }

    /// <inheritdoc />
    public virtual bool Playing
    {
        get
        {
            _checkDisposed();
            var state = AL.GetSourceState(SourceHandle);
            Master._checkAlError();
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


            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public bool Looping
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourceb.Looping, out var ret);
            Master._checkAlError();
            return ret;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourceb.Looping, value);
            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public bool Global
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourceb.SourceRelative, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourceb.SourceRelative, value);
            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public virtual Vector2 Position
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSource3f.Position, out var x, out var y, out _);
            Master._checkAlError();
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

            AL.Source(SourceHandle, ALSource3f.Position, x, y, 0);
            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float Pitch
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.Pitch, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.Pitch, value);
            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float Volume
    {
        get
        {
            var gain = Gain;
            var volume = SharedAudioSystem.GainToVolume(gain);
            return volume;
        }
        set => Gain = SharedAudioSystem.VolumeToGain(value);
    }

    /// <inheritdoc />
    public float Gain
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.Gain, out var gain);
            Master._checkAlError();
            return gain;
        }
        set
        {
            if (float.IsNaN(value))
            {
                Master.LogError($"Tried to set NaN gain, setting audio source to 0f: {Environment.StackTrace}");
                value = 0f;
            }

            _checkDisposed();
            var priorOcclusion = 1f;
            if (!IsEfxSupported)
            {
                AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
                // Default to 0 to avoid spiking audio, just means it will be muted for a frame in this case.
                priorOcclusion = _gain == 0 ? 1f : priorGain / _gain;
            }

            _gain = value;
            AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
            Master.LogALError($"Gain is {_gain:0.00} and priorOcclusion is {priorOcclusion:0.00}. EFX supported: {IsEfxSupported}");
        }
    }

    /// <inheritdoc />
    public float MaxDistance
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.MaxDistance, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.MaxDistance, value);
            Master.LogALError($"MaxDistance is {value:0.00}");
        }
    }

    /// <inheritdoc />
    public float RolloffFactor
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.RolloffFactor, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.RolloffFactor, value);
            Master.LogALError($"RolloffFactor is {value:0.00}");
        }
    }

    /// <inheritdoc />
    public float ReferenceDistance
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.ReferenceDistance, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            AL.Source(SourceHandle, ALSourcef.ReferenceDistance, value);
            Master.LogALError($"ReferenceDistance is {value:0.00}");
        }
    }

    /// <inheritdoc />
    public float Occlusion
    {
        get => _occlusion;
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

            _occlusion = value;
            Master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float PlaybackPosition
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSourcef.SecOffset, out var value);
            Master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();

            value = MathF.Max(value, 0f);
            AL.Source(SourceHandle, ALSourcef.SecOffset, value);
            Master._checkAlError($"Tried to set invalid playback position of {value:0.00}");
        }
    }

    /// <inheritdoc />
    public Vector2 Velocity
    {
        get
        {
            _checkDisposed();

            AL.GetSource(SourceHandle, ALSource3f.Velocity, out var x, out var y, out _);
            Master._checkAlError();
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
            Master._checkAlError();
        }
    }

    void IAudioSource.SetAuxiliary(IAuxiliaryAudio? audio)
    {
        _checkDisposed();
        if (!IsEfxSupported)
            return;

        if (audio is AuxiliaryAudio impAudio)
        {
            EFX.Source(SourceHandle, EFXSourceInteger3.AuxiliarySendFilter, impAudio.Handle, 0, 0);
        }
        else
        {
            EFX.Source(SourceHandle, EFXSourceInteger3.AuxiliarySendFilter, 0, 0, 0);
        }

        Master._checkAlError();
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

    protected static bool AreFinite(float x, float y)
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

    protected abstract void Dispose(bool disposing);

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
