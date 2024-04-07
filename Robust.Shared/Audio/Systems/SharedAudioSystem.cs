using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio.Systems;

/// <summary>
/// Handles audio for robust toolbox inside of the sim.
/// </summary>
/// <remarks>
/// Interacts with AudioManager internally.
/// </remarks>
public abstract partial class SharedAudioSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly INetManager _netManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly IRobustRandom RandMan = default!;

    /// <summary>
    /// Default max range at which the sound can be heard.
    /// </summary>
    public const float DefaultSoundRange = 20;

    /// <summary>
    /// Used in the PAS to designate the physics collision mask of occluders.
    /// </summary>
    public int OcclusionCollisionMask { get; set; }

    public virtual float ZOffset { get; protected set; }

    public override void Initialize()
    {
        base.Initialize();
        InitializeEffect();
        ZOffset = CfgManager.GetCVar(CVars.AudioZOffset);
        Subs.CVar(CfgManager, CVars.AudioZOffset, SetZOffset);
        SubscribeLocalEvent<AudioComponent, ComponentGetStateAttemptEvent>(OnAudioGetStateAttempt);
        SubscribeLocalEvent<AudioComponent, EntityUnpausedEvent>(OnAudioUnpaused);
    }

    protected void SetZOffset(float value)
    {
        ZOffset = value;
    }

    protected virtual void OnAudioUnpaused(EntityUid uid, AudioComponent component, ref EntityUnpausedEvent args)
    {
        component.AudioStart += args.PausedTime;
    }

    private void OnAudioGetStateAttempt(EntityUid uid, AudioComponent component, ref ComponentGetStateAttemptEvent args)
    {
        var playerEnt = args.Player?.AttachedEntity;

        if (component.ExcludedEntity != null && playerEnt == component.ExcludedEntity)
        {
            args.Cancelled = true;
            return;
        }

        if (playerEnt != null && component.IncludedEntities != null && !component.IncludedEntities.Contains(playerEnt.Value))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Considers Z-offset for audio and gets the adjusted distance.
    /// </summary>
    /// <remarks>
    /// Really it's just doing pythagoras for you.
    /// </remarks>
    public float GetAudioDistance(float length)
    {
        return MathF.Sqrt(MathF.Pow(length, 2) + MathF.Pow(ZOffset, 2));
    }

    /// <summary>
    /// Resolves the filepath to a sound file.
    /// </summary>
    public string GetSound(SoundSpecifier specifier)
    {
        switch (specifier)
        {
            case SoundPathSpecifier path:
                return path.Path == default ? string.Empty : path.Path.ToString();

            case SoundCollectionSpecifier collection:
            {
                if (collection.Collection == null)
                    return string.Empty;

                var soundCollection = ProtoMan.Index<SoundCollectionPrototype>(collection.Collection);
                return RandMan.Pick(soundCollection.PickFiles).ToString();
            }
        }

        return string.Empty;
    }

    #region AudioParams

    protected AudioComponent SetupAudio(EntityUid uid, string? fileName, AudioParams? audioParams, TimeSpan? length = null)
    {
        DebugTools.Assert((!string.IsNullOrEmpty(fileName) || length is not null));
        audioParams ??= AudioParams.Default;
        var comp = AddComp<AudioComponent>(uid);
        comp.FileName = fileName ?? string.Empty;
        comp.Params = audioParams.Value;
        comp.AudioStart = Timing.CurTime;


        if (!audioParams.Value.Loop)
        {
            length ??= GetAudioLength(fileName!);

            var despawn = AddComp<TimedDespawnComponent>(uid);
            // Don't want to clip audio too short due to imprecision.
            despawn.Lifetime = (float) length.Value.TotalSeconds + 0.01f;
        }

        if (comp.Params.Variation != null && comp.Params.Variation.Value != 0f)
        {
            comp.Params.Pitch *= (float) RandMan.NextGaussian(1, comp.Params.Variation.Value);
        }

        return comp;
    }

    public static float GainToVolume(float value)
    {
        if (value < 0f)
        {
            value = 0f;
        }

        return 10f * MathF.Log10(value);
    }

    public static float VolumeToGain(float value)
    {
        var result = MathF.Pow(10, value / 10);

        if (result < 0f)
        {
            throw new InvalidOperationException($"Tried to get gain calculation that resulted in invalid value of {result}");
        }

        return result;
    }

    /// <summary>
    /// Sets the audio params volume for an entity.
    /// </summary>
    public void SetGain(EntityUid? entity, float value, AudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component))
            return;

        var volume = GainToVolume(value);
        SetVolume(entity, volume, component);
    }

    /// <summary>
    /// Sets the audio params volume for an entity.
    /// </summary>
    public void SetVolume(EntityUid? entity, float value, AudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component))
            return;

        if (component.Params.Volume.Equals(value))
            return;

        component.Params.Volume = value;
        component.Volume = value;
        Dirty(entity.Value, component);
    }

    #endregion

    /// <summary>
    /// Gets the timespan of the specified audio.
    /// </summary>
    public TimeSpan GetAudioLength(string filename)
    {
        if (!filename.StartsWith("/"))
            throw new ArgumentException("Path must be rooted");

        return GetAudioLengthImpl(filename);
    }

    protected abstract TimeSpan GetAudioLengthImpl(string filename);

    /// <summary>
    /// Stops the specified audio entity from playing.
    /// </summary>
    /// <remarks>
    /// Returns null so you can inline the call.
    /// </remarks>
    public EntityUid? Stop(EntityUid? uid, Components.AudioComponent? component = null)
    {
        // One morbillion warnings for logging missing.
        if (uid == null || !Resolve(uid.Value, ref component, false))
            return null;

        if (!Timing.IsFirstTimePredicted || (_netManager.IsClient && !IsClientSide(uid.Value)))
            return null;

        QueueDel(uid);
        return null;
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(string? filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(SoundSpecifier? sound, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), playerFilter, recordReplay, sound.Params);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(string? filename, ICommonSession recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(SoundSpecifier? sound, ICommonSession recipient)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, sound.Params);
    }

    public abstract void LoadStream<T>(Entity<AudioComponent> entity, T stream);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(string? filename, EntityUid recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayGlobal(SoundSpecifier? sound, EntityUid recipient, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(string? filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(string? filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(string? filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(SoundSpecifier? sound, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), playerFilter, uid, recordReplay, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(SoundSpecifier? sound, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayEntity(SoundSpecifier? sound, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayPvs(SoundSpecifier? sound, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayPvs(GetSound(sound), uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at the specified EntityCoordinates for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The EntityCoordinates to attach the audio source to.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayPvs(SoundSpecifier? sound, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayPvs(GetSound(sound), coordinates, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at the specified EntityCoordinates for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The EntityCoordinates to attach the audio source to.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayPvs(string? filename,
        EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayPvs(string? filename, EntityUid uid,
        AudioParams? audioParams = null);

    /// <summary>
    /// Plays a predicted sound following an entity. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="source">The UID of the entity "emitting" the audio.</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    [return: NotNullIfNotNull("sound")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null);

    /// <summary>
    /// Plays a predicted sound following an EntityCoordinates. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The entitycoordinates "emitting" the audio</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    [return: NotNullIfNotNull("sound")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(string? filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(string? filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("filename")]
    public abstract (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(string? filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(SoundSpecifier? sound, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), playerFilter, coordinates, recordReplay);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(SoundSpecifier? sound, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    [return: NotNullIfNotNull("sound")]
    public (EntityUid Entity, Components.AudioComponent Component)? PlayStatic(SoundSpecifier? sound, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, audioParams ?? sound.Params);
    }

    // These are just here for replays now.
    // We don't actually need them in shared, or netserializable, but this makes net serialization
    // and replays happy

    // TODO: This is quite bandwidth intensive.
    // Sending bus names and file names as strings is expensive and can be optimized.
    // Also there's redundant fields in AudioParams in most cases.
    [NetSerializable, Serializable]
    protected abstract class AudioMessage : EntityEventArgs
    {
        public string FileName = string.Empty;
        public AudioParams AudioParams;
    }

    [NetSerializable, Serializable]
    protected sealed class PlayAudioGlobalMessage : AudioMessage
    {
    }

    [NetSerializable, Serializable]
    protected sealed class PlayAudioPositionalMessage : AudioMessage
    {
        public NetCoordinates Coordinates;
    }

    [NetSerializable, Serializable]
    protected sealed class PlayAudioEntityMessage : AudioMessage
    {
        public NetEntity NetEntity;
    }
}
