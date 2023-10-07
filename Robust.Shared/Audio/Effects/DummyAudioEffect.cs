using Robust.Shared.Audio.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.Audio.Effects;

/// <inheritdoc />
internal sealed class DummyAudioEffect : IAudioEffect
{
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public float Density { get; set; }

    /// <inheritdoc />
    public float Diffusion { get; set; }

    /// <inheritdoc />
    public float Gain { get; set; }

    /// <inheritdoc />
    public float GainHF { get; set; }

    /// <inheritdoc />
    public float GainLF { get; set; }

    /// <inheritdoc />
    public float DecayTime { get; set; }

    /// <inheritdoc />
    public float DecayHFRatio { get; set; }

    /// <inheritdoc />
    public float DecayLFRatio { get; set; }

    /// <inheritdoc />
    public float ReflectionsGain { get; set; }

    /// <inheritdoc />
    public float ReflectionsDelay { get; set; }

    /// <inheritdoc />
    public Vector3 ReflectionsPan { get; set; }

    /// <inheritdoc />
    public float LateReverbGain { get; set; }

    /// <inheritdoc />
    public float LateReverbDelay { get; set; }

    /// <inheritdoc />
    public Vector3 LateReverbPan { get; set; }

    /// <inheritdoc />
    public float EchoTime { get; set; }

    /// <inheritdoc />
    public float EchoDepth { get; set; }

    /// <inheritdoc />
    public float ModulationTime { get; set; }

    /// <inheritdoc />
    public float ModulationDepth { get; set; }

    /// <inheritdoc />
    public float AirAbsorptionGainHF { get; set; }

    /// <inheritdoc />
    public float HFReference { get; set; }

    /// <inheritdoc />
    public float LFReference { get; set; }

    /// <inheritdoc />
    public float RoomRolloffFactor { get; set; }

    /// <inheritdoc />
    public int DecayHFLimit { get; set; }
}
