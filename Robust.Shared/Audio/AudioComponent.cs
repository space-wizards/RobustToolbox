using Robust.Shared.Audio.Sources;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores the audio data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAudioSystem))]
public sealed partial class AudioComponent : Component
{
    /// <summary>
    /// Audio source that interacts with OpenAL.
    /// Null on server.
    /// </summary>
    internal IAudioSource Source = default!;

    public bool IsPlaying { get; }
    public bool Done { get; set; }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            Source.SetVolume(value);
        }
    }

    private float _volume;

    public float MaxDistance;
    public float ReferenceDistance;
    public float RolloffFactor;

    public Attenuation Attenuation
    {
        get => _attenuation;
        set
        {
            if (value == _attenuation) return;
            _attenuation = value;
            if (_attenuation != Attenuation.Default)
            {
                // Need to disable default attenuation when using a custom one
                // Damn Sloth wanting linear ambience sounds so they smoothly cut-off and are short-range
                Source.SetRolloffFactor(0f);
            }
        }
    }
    private Attenuation _attenuation = Attenuation.Default;

    public void Stop()
    {
        Source.StopPlaying();
    }

    public void Dispose()
    {
        Source.Dispose();
    }

    public AudioType AudioType = AudioType.Local;

    [AutoNetworkedField]
    public string FileName;

    [AutoNetworkedField]
    public AudioParams Params = AudioParams.Default;
}

public enum AudioType : byte
{
    /// <summary>
    /// Audio will have its position set to the parent entity every frame.
    /// </summary>
    Local,

    /// <summary>
    /// Audio will have its position set to the listener every frame.
    /// </summary>
    Global,
}
