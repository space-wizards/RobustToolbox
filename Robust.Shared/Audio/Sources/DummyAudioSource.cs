using System.Numerics;

namespace Robust.Shared.Audio.Sources;

/// <summary>
///     Hey look, it's AudioSource's evil twin brother!
/// </summary>
[Virtual]
internal class DummyAudioSource : IAudioSource
{
    public static DummyAudioSource Instance { get; } = new();

    public void Dispose()
    {
    }

    /// <inheritdoc />
    public bool Playing { get; set; }

    /// <inheritdoc />
    public bool Looping { get; set; }

    /// <inheritdoc />
    public bool Global { get; set; }

    /// <inheritdoc />
    public Vector2 Position { get; set; }

    /// <inheritdoc />
    public float Pitch { get; set; }

    /// <inheritdoc />
    public float Volume { get; set; }

    /// <inheritdoc />
    public float Gain { get; set; }

    /// <inheritdoc />
    public float MaxDistance { get; set; }

    /// <inheritdoc />
    public float RolloffFactor { get; set; }

    /// <inheritdoc />
    public float ReferenceDistance { get; set; }

    /// <inheritdoc />
    public float Occlusion { get; set; }

    /// <inheritdoc />
    public float PlaybackPosition { get; set; }

    /// <inheritdoc />
    public Vector2 Velocity { get; set; }
}
