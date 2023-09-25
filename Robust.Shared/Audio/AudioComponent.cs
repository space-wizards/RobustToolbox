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
     * Most of these aren't networked as they double AudioParams data and these just interact with IAudioSource.
     */

    #region Source

    /// <summary>
    /// Starts playing if the source is not already playing.
    /// </summary>
    public void StartPlaying() => Source.StartPlaying();

    /// <summary>
    /// <see cref="IAudioSource.Playing"/>
    /// </summary>
    [ViewVariables]
    public bool Playing
    {
        get => Source.Playing;
        set => Source.Playing = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Looping"/>
    /// </summary>
    [ViewVariables]
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
    /// <see cref="IAudioSource.Pitch"/>
    /// </summary>
    public float Pitch
    {
        get => Source.Pitch;
        set => Source.Pitch = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.MaxDistance"/>
    /// </summary>
    public float MaxDistance
    {
        get => Params.MaxDistance;
        set => Params.MaxDistance = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.RolloffFactor"/>
    /// </summary>
    public float RolloffFactor
    {
        get => Params.RolloffFactor;
        set => Params.RolloffFactor = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.ReferenceDistance"/>
    /// </summary>
    public float ReferenceDistance
    {
        get => Params.ReferenceDistance;
        set => Params.ReferenceDistance = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Position"/>
    /// </summary>
    /// <remarks>
    /// Not replicated as audio always tracks the entity's position.
    /// </remarks>
    [ViewVariables]
    public Vector2 Position
    {
        get => Source.Position;
        set => Source.Position = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Volume"/>
    /// </summary>
    [ViewVariables]
    public float Volume
    {
        get => Source.Volume;
        set => Source.Volume = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Gain"/>
    /// </summary>
    [ViewVariables]
    public float Gain
    {
        get => Source.Gain;
        set => Source.Gain = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Occlusion"/>
    /// </summary>
    [ViewVariables]
    public float Occlusion
    {
        get => Source.Occlusion;
        set => Source.Occlusion = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.PlaybackPosition"/>
    /// </summary>
    [ViewVariables]
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
    [ViewVariables]
    public Vector2 Velocity
    {
        get => Source.Velocity;
        set => Source.Velocity = value;
    }

    #endregion

    public void Dispose()
    {
        Source.Dispose();
    }
}
