using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Stores the audio data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true), Access(typeof(SharedAudioSystem))]
public sealed partial class AudioComponent : Component, IAudioSource
{
    [AutoNetworkedField, DataField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public AudioFlags Flags = AudioFlags.None;

    #region Filter

    public override bool SessionSpecific => true;

    /// <summary>
    /// Used for synchronising audio on client that comes into PVS range.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan AudioStart;

    // Don't need to network these as client doesn't care.

    /// <summary>
    /// If this sound was predicted do we exclude it from a specific entity.
    /// Useful for predicted audio.
    /// </summary>
    [DataField]
    public EntityUid? ExcludedEntity;

    /// <summary>
    /// If the sound was filtered what entities were included.
    /// </summary>
    [DataField]
    public HashSet<EntityUid>? IncludedEntities;

    #endregion

    // We can't just start playing on audio creation as we don't have the correct position yet.
    // As such we'll wait for FrameUpdate before we start playing to avoid the position being cooked.
    public bool Started = false;

    [AutoNetworkedField]
    [DataField(required: true)]
    public string FileName = string.Empty;

    public bool Loaded = false;

    /// <summary>
    /// Audio params. Set this if you want to adjust default volume, max distance, etc.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public AudioParams Params = AudioParams.Default;

    /// <summary>
    /// Audio source that interacts with OpenAL.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    internal IAudioSource Source = new DummyAudioSource();

    /// <summary>
    /// Auxiliary entity to pass audio to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Auxiliary;

    /*
     * Values for IAudioSource stored on the component and sent to IAudioSource as applicable.
     * Most of these aren't networked as they double AudioParams data and these just interact with IAudioSource.
     */

    #region Source

    public void Pause() => Source.Pause();

    /// <inheritdoc />
    public void StartPlaying() => Source.StartPlaying();

    /// <inheritdoc />
    public void StopPlaying()
    {
        PlaybackPosition = 0f;
        Source.StopPlaying();
    }

    /// <inheritdoc />
    public void Restart() => Source.Restart();

    [DataField, AutoNetworkedField]
    public AudioState State = AudioState.Playing;

    /// <summary>
    /// Time when the audio was paused so we can offset it later if relevant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? PauseTime;

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
    [Access(typeof(SharedAudioSystem))]
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
    [Access(Other = AccessPermissions.ReadWriteExecute)]
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

    void IAudioSource.SetAuxiliary(IAuxiliaryAudio? audio)
    {
        Source.SetAuxiliary(audio);
    }

    #endregion

    public void Dispose()
    {
        Source.Dispose();
    }
}

[Serializable, NetSerializable]
public enum AudioState : byte
{
    Stopped,
    Playing,
    Paused,
}

[Flags]
public enum AudioFlags : byte
{
    None = 0,

    /// <summary>
    /// Should the audio act as if attached to a grid?
    /// </summary>
    GridAudio = 1 << 0,

    NoOcclusion = 1 << 1,
}
