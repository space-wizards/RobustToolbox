using System.Numerics;
using Robust.Shared.Audio.Sources;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores the audio data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAudioSystem))]
public sealed partial class AudioComponent : Component, IAudioSource
{
    [AutoNetworkedField]
    [DataField]
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
                Source.RolloffFactor = 0f;
            }
        }
    }

    private Attenuation _attenuation = Attenuation.Default;

    [AutoNetworkedField]
    [DataField]
    public string FileName;

    [AutoNetworkedField]
    [DataField]
    public AudioParams Params = AudioParams.Default;

    /// <summary>
    /// Audio source that interacts with OpenAL.
    /// </summary>
    internal IAudioSource Source = default!;

    /*
     * Values for IAudioSource stored on the component and sent to IAudioSource as applicable.
     */

    #region Source

    /// <summary>
    /// <see cref="IAudioSource.Playing"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool Playing
    {
        get => Source.Playing;
        set => Source.Playing = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Looping"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool Looping { get; set; }

    /// <summary>
    /// <see cref="IAudioSource.Global"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool Global { get; set; }

    /// <summary>
    /// <see cref="IAudioSource.Position"/>
    /// </summary>
    /// <remarks>
    /// Not replicated as audio always tracks the entity's position.
    /// </remarks>
    public Vector2 Position
    {
        get => Source.Position;
        set => Source.Position = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Pitch"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float Pitch { get; set; }

    [AutoNetworkedField]
    [DataField]
    public float Volume { get; set; }

    [AutoNetworkedField]
    [DataField]
    public float Gain { get; set; }

    [AutoNetworkedField]
    [DataField]
    public float MaxDistance { get; set; }

    [AutoNetworkedField]
    [DataField]
    public float RolloffFactor { get; set; }

    [AutoNetworkedField]
    [DataField]
    public float ReferenceDistance { get; set; }


    [AutoNetworkedField]
    [DataField]
    public float Occlusion { get; set; }

    /// <summary>
    /// <see cref="IAudioSource.PlaybackPosition"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float PlaybackPosition { get; set; }

    /// <summary>
    /// <see cref="IAudioSource.Velocity"/>
    /// </summary>
    /// <remarks>
    /// Not replicated.
    /// </remarks>
    public Vector2 Velocity { get; set; }

    #endregion

    public void Dispose()
    {
        Source?.Dispose();
    }
}
