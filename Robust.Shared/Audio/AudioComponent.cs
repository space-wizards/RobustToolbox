using System.Numerics;
using Robust.Shared.Audio.Sources;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores the audio data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAudioSystem))]
public sealed partial class AudioComponent : Component, IAudioSource
{
    private Attenuation _attenuation = Attenuation.Default;

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

    [AutoNetworkedField]
    [DataField]
    public string FileName;

    [AutoNetworkedField]
    [DataField]
    public AudioParams Params = AudioParams.Default;

    /// <summary>
    /// Used on engine to determine every frame if audio is done playing.
    /// </summary>
    internal bool Done;

    /// <summary>
    /// Audio source that interacts with OpenAL.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    internal IAudioSource Source = default!;

    /*
     * Values for IAudioSource stored on the component and sent to IAudioSource as applicable.
     */

    #region Source

    /// <summary>
    /// Starts playing if the source is not already playing.
    /// </summary>
    public void StartPlaying()
    {
        if (Playing)
            return;

        Playing = true;
    }

    /// <summary>
    /// <see cref="IAudioSource.Playing"/>
    /// </summary>
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
    public bool Looping
    {
        get => Source.Looping;
        set => Source.Looping = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Global"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool Global
    {
        get => Source.Global;
        set => Source.Global = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Position"/>
    /// </summary>
    /// <remarks>
    /// Not replicated as audio always tracks the entity's position.
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
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
    public float Pitch
    {
        get => Source.Pitch;
        set => Source.Pitch = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Volume"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float Volume
    {
        get => Source.Volume;
        set => Source.Volume = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Gain"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float Gain
    {
        get => Source.Gain;
        set => Source.Gain = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.MaxDistance"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float MaxDistance
    {
        get => Source.MaxDistance;
        set => Source.MaxDistance = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.RolloffFactor"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float RolloffFactor
    {
        get => Source.RolloffFactor;
        set => Source.RolloffFactor = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.ReferenceDistance"/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public float ReferenceDistance
    {
        get => Source.ReferenceDistance;
        set => Source.ReferenceDistance = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Occlusion"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float Occlusion
    {
        get => Source.Occlusion;
        set => Source.Occlusion = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.PlaybackPosition"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float PlaybackPosition
    {
        get => Source.PlaybackPosition;
        set => Source.PlaybackPosition = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Velocity"/>
    /// </summary>
    /// <remarks>
    /// Not replicated.
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 Velocity
    {
        get => Source.Velocity;
        set => Source.Velocity = value;
    }

    #endregion

    public void Dispose()
    {
        Source?.Dispose();
    }
}
