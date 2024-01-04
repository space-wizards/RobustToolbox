using System.Numerics;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio.Sources;

/// <summary>
///     Hey look, it's AudioSource's evil twin brother!
/// </summary>
[Virtual]
[DataDefinition]
internal partial class DummyAudioSource : IAudioSource
{
    public static DummyAudioSource Instance { get; } = new();

    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void Pause()
    {
    }

    /// <inheritdoc />
    public void StartPlaying()
    {
    }

    /// <inheritdoc />
    public void StopPlaying()
    {
    }

    /// <inheritdoc />
    public void Restart()
    {
    }

    /// <inheritdoc />
    public bool Playing { get; set; }

    /// <inheritdoc />
    [DataField]
    public bool Looping { get; set; }

    /// <inheritdoc />
    [DataField]
    public bool Global { get; set; }

    /// <inheritdoc />
    public Vector2 Position { get; set; }

    /// <inheritdoc />
    [DataField]
    public float Pitch { get; set; }

    /// <inheritdoc />
    public float Volume { get; set; }

    /// <inheritdoc />
    public float Gain { get; set; }

    /// <inheritdoc />
    [DataField]
    public float MaxDistance { get; set; }

    /// <inheritdoc />
    [DataField]
    public float RolloffFactor { get; set; }

    /// <inheritdoc />
    [DataField]
    public float ReferenceDistance { get; set; }

    /// <inheritdoc />
    public float Occlusion { get; set; }

    /// <inheritdoc />
    public float PlaybackPosition { get; set; }

    /// <inheritdoc />
    public Vector2 Velocity { get; set; }

    public void SetAuxiliary(IAuxiliaryAudio? audio)
    {
    }
}
