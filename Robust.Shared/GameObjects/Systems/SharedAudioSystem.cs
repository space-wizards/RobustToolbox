using System;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;
public abstract class SharedAudioSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] protected readonly IRobustRandom RandMan = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;

    private Dictionary<string, TimeSpan> _audioLengths = new();

    /*
     * TODO: Maybe just fuck it re-do everything.
     * Shared needs a way to cache and get audio length.
     * Should be able to derive creationtick > creation time of audio and
     * Maybe make ISharedResourceCache and have some audio in shared ig.
     */

    /// <summary>
    /// Just so we can have an entity that doesn't get serialized.
    /// </summary>
    protected static readonly EntProtoId AudioEntity = new("AudioEntity");

    /// <summary>
    /// Default max range at which the sound can be heard.
    /// </summary>
    public const float DefaultSoundRange = 25;

    /// <summary>
    /// Used in the PAS to designate the physics collision mask of occluders.
    /// </summary>
    public int OcclusionCollisionMask { get; set; }

    /// <summary>
    /// Resolves the filepath to a sound file.
    /// </summary>
    /// <param name="specifier"></param>
    /// <returns></returns>
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

                var soundCollection = _protoMan.Index<SoundCollectionPrototype>(collection.Collection);
                return RandMan.Pick(soundCollection.PickFiles).ToString();
            }
        }

        return string.Empty;
    }

    public TimeSpan GetAudioLength(string filename)
    {

    }

    /// <summary>
    /// Stops the specified audio entity from playing.
    /// </summary>
    public void Stop(EntityUid uid, AudioComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        QueueDel(uid);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, Filter playerFilter, bool recordReplay)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), playerFilter, recordReplay, sound.Params);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, ICommonSession recipient)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, sound.Params);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, EntityUid recipient)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, Filter playerFilter, EntityUid uid, bool recordReplay)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), playerFilter, uid, recordReplay, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, ICommonSession recipient, EntityUid uid)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, EntityUid recipient, EntityUid uid)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayPvs(SoundSpecifier? sound, EntityUid uid)
    {
        return sound == null ? null : PlayPvs(GetSound(sound), uid, sound.Params);
    }

    /// <summary>
    /// Play an audio file at the specified EntityCoordinates for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The EntityCoordinates to attach the audio source to.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayPvs(SoundSpecifier? sound, EntityCoordinates coordinates)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), Filter.Pvs(coordinates, entityMan: EntityManager, playerMan: PlayerManager), coordinates, true, sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayPvs(string filename, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, Filter.Pvs(uid, entityManager: EntityManager, playerManager:PlayerManager, cfgManager:CfgManager), uid, true, audioParams);
    }

    /// <summary>
    /// Plays a predicted sound following an entity. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="source">The UID of the entity "emitting" the audio.</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user);

    /// <summary>
    /// Plays a predicted sound following an EntityCoordinates. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The entitycoordinates "emitting" the audio</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public abstract (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(SoundSpecifier? sound, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), playerFilter, coordinates, recordReplay);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(SoundSpecifier? sound, ICommonSession recipient, EntityCoordinates coordinates)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, sound.Params);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    public (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(SoundSpecifier? sound, EntityUid recipient, EntityCoordinates coordinates)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, sound.Params);
    }
}
