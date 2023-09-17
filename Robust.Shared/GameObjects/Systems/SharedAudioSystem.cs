using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Robust.Shared.GameObjects;
public abstract class SharedAudioSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private   readonly IMapManager _mapManager = default!;
    [Dependency] private   readonly IPrototypeManager _protoMan = default!;
    [Dependency] protected readonly IRobustRandom RandMan = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] private   readonly SharedMapSystem _map = default!;

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

    /// <summary>
    /// Stops the specified audio entity from playing.
    /// </summary>
    public void Stop(EntityUid uid, AudioComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        QueueDel(uid);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, Filter playerFilter, bool recordReplay)
    {
        return sound == null ? null : PlayGlobal(sound, playerFilter, recordReplay);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, ICommonSession recipient)
    {
        return sound == null ? null : PlayGlobal(sound, recipient);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayGlobal(SoundSpecifier? sound, EntityUid recipient)
    {
        return sound == null ? null : PlayGlobal(sound, recipient);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, Filter playerFilter, EntityUid uid, bool recordReplay)
    {
        return sound == null ? null : Play(sound, playerFilter, uid, recordReplay);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayEntity(SoundSpecifier? sound, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayPvs(SoundSpecifier? sound, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayPvs(GetSound(sound), uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at the specified EntityCoordinates for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="uid">The EntityCoordinates to attach the audio source to.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayPvs(SoundSpecifier? sound, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return sound == null ? null : Play(GetSound(sound), Filter.Pvs(coordinates, entityMan: EntityManager, playerMan: PlayerManager), coordinates, true, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayPvs(string filename, EntityUid uid, AudioParams? audioParams = null)
    {
        return Play(filename, Filter.Pvs(uid, entityManager: EntityManager, playerManager:PlayerManager, cfgManager:CfgManager), uid, true, audioParams);
    }

    /// <summary>
    /// Plays a predicted sound following an entity. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="source">The UID of the entity "emitting" the audio.</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public abstract IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null);

    /// <summary>
    /// Plays a predicted sound following an EntityCoordinates. The server will send the sound to every player in PVS range,
    /// unless that player is attached to the "user" entity that initiated the sound. The client-side system plays
    /// this sound as normal
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="coordinates">The entitycoordinates "emitting" the audio</param>
    /// <param name="user">The UID of the user that initiated this sound. This is usually some player's controlled entity.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public abstract IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user);

    // TODO rename to play static
    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates);

    // TODO rename to play static
    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? Play(SoundSpecifier? sound, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay)
    {
        return sound == null ? null : Play(GetSound(sound), playerFilter, coordinates, recordReplay, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? PlayStatic(SoundSpecifier? sound, ICommonSession recipient, EntityCoordinates coordinates)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public (EntityUid uid, IPlayingAudioStream Stream)? PlayStatic(SoundSpecifier? sound, EntityUid recipient, EntityCoordinates coordinates)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, audioParams ?? sound.Params);
    }
}
