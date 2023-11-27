using System;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Maths;

namespace Robust.Client.Audio.Effects;

/// <inheritdoc />
internal sealed class AudioEffect : IAudioEffect
{
    internal int Handle;

    private readonly IAudioInternal _master;

    public AudioEffect(IAudioInternal manager)
    {
        Handle = EFX.GenEffect();
        _master = manager;
        EFX.Effect(Handle, EffectInteger.EffectType, (int) EffectType.EaxReverb);
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            EFX.DeleteEffect(Handle);
            Handle = 0;
        }
    }

    private void _checkDisposed()
    {
        if (Handle == -1)
        {
            throw new ObjectDisposedException(nameof(AudioEffect));
        }
    }

    /// <inheritdoc />
    public float Density
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbDensity, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbDensity, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float Diffusion
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbDiffusion, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbDiffusion, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float Gain
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbGain, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbGain, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float GainHF
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbGainHF, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbGainHF, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float GainLF
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbGainLF, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbGainLF, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float DecayTime
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbDecayTime, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbDecayTime, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float DecayHFRatio
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbDecayHFRatio, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbDecayHFRatio, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float DecayLFRatio
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbDecayLFRatio, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbDecayLFRatio, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float ReflectionsGain
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbReflectionsGain, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbReflectionsGain, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float ReflectionsDelay
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbReflectionsDelay, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbReflectionsDelay, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public Vector3 ReflectionsPan
    {
        get
        {
            _checkDisposed();
            var value = EFX.GetEffect(Handle, EffectVector3.EaxReverbReflectionsPan);
            _master._checkAlError();
            return new Vector3(value.X, value.Z, value.Y);
        }
        set
        {
            _checkDisposed();
            var openVec = new OpenTK.Mathematics.Vector3(value.X, value.Y, value.Z);
            EFX.Effect(Handle, EffectVector3.EaxReverbReflectionsPan, ref openVec);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float LateReverbGain
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbLateReverbGain, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbLateReverbGain, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float LateReverbDelay
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbLateReverbDelay, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbLateReverbDelay, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public Vector3 LateReverbPan
    {
        get
        {
            _checkDisposed();
            var value = EFX.GetEffect(Handle, EffectVector3.EaxReverbLateReverbPan);
            _master._checkAlError();
            return new Vector3(value.X, value.Z, value.Y);
        }
        set
        {
            _checkDisposed();
            var openVec = new OpenTK.Mathematics.Vector3(value.X, value.Y, value.Z);
            EFX.Effect(Handle, EffectVector3.EaxReverbLateReverbPan, ref openVec);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float EchoTime
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbEchoTime, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbEchoTime, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float EchoDepth
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbEchoDepth, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbEchoDepth, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float ModulationTime
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbModulationTime, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbModulationTime, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float ModulationDepth
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbModulationDepth, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbModulationDepth, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float AirAbsorptionGainHF
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbAirAbsorptionGainHF, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbAirAbsorptionGainHF, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float HFReference
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbHFReference, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbHFReference, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float LFReference
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbLFReference, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbLFReference, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public float RoomRolloffFactor
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectFloat.EaxReverbRoomRolloffFactor, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectFloat.EaxReverbRoomRolloffFactor, value);
            _master._checkAlError();
        }
    }

    /// <inheritdoc />
    public int DecayHFLimit
    {
        get
        {
            _checkDisposed();
            EFX.GetEffect(Handle, EffectInteger.EaxReverbDecayHFLimit, out var value);
            _master._checkAlError();
            return value;
        }
        set
        {
            _checkDisposed();
            EFX.Effect(Handle, EffectInteger.EaxReverbDecayHFLimit, value);
            _master._checkAlError();
        }
    }
}
