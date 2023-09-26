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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedAudioSystem))]
public sealed partial class AudioComponent : Component, IAudioSource
{
    #region Filter

    public override bool SessionSpecific => true;

    /// <summary>
    /// If this sound was predicted do we exclude it from a specific entity.
    /// </summary>
    public EntityUid? ExcludedEntity;

    #endregion

    [AutoNetworkedField]
    [DataField]
    public string FileName;

    /// <summary>
    /// Audio params. Set this if you want to adjust default volume, max distance, etc.
    /// </summary>
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

    public void Pause() => Source.Pause();

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
    public bool Global { get; set; }

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
        get => Source.MaxDistance;
        set => Source.MaxDistance = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.RolloffFactor"/>
    /// </summary>
    public float RolloffFactor
    {
        get => Source.RolloffFactor;
        set => Source.RolloffFactor = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.ReferenceDistance"/>
    /// </summary>
    public float ReferenceDistance
    {
        get => Source.ReferenceDistance;
        set => Source.ReferenceDistance = value;
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
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float Volume
    {
        get => Source.Volume;
        set => Source.Volume = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Gain"/>
    /// </summary>
    [ViewVariables]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float Gain
    {
        get => Source.Gain;
        set => Source.Gain = value;
    }

    /// <summary>
    /// <see cref="IAudioSource.Occlusion"/>
    /// </summary>
    [ViewVariables]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
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
