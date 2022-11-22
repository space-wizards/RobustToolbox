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
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] protected readonly IRobustRandom RandMan = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;

    /// <summary>
    /// Default max range at which the sound can be heard.
    /// </summary>
    public const int DefaultSoundRange = 25;

    /// <summary>
    /// Used in the PAS to designate the physics collision mask of occluders.
    /// </summary>
    public int OcclusionCollisionMask { get; set; }

    public string GetSound(SoundSpecifier specifier)
    {
        switch (specifier)
        {
            case SoundPathSpecifier path:
                return path.Path == null ? string.Empty : path.Path.ToString();

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
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null);


    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? PlayGlobal(SoundSpecifier? sound, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), playerFilter, recordReplay, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? PlayGlobal(SoundSpecifier? sound, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file globally, without position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? PlayGlobal(SoundSpecifier? sound, EntityUid recipient, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayGlobal(GetSound(sound), recipient, audioParams ?? sound.Params);
    }

    // TODO rename to PlayEntity
    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null);

    // TODO rename to PlayEntity
    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public IPlayingAudioStream? Play(SoundSpecifier? sound, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        return sound == null ? null : Play(GetSound(sound), playerFilter, uid, recordReplay, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public IPlayingAudioStream? PlayEntity(SoundSpecifier? sound, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
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
    public IPlayingAudioStream? PlayEntity(SoundSpecifier? sound, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayEntity(GetSound(sound), recipient, uid, audioParams ?? sound.Params);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public IPlayingAudioStream? PlayPvs(SoundSpecifier? sound, EntityUid uid, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayPvs(GetSound(sound), uid, audioParams);
    }

    /// <summary>
    /// Play an audio file following an entity for every entity in PVS range.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="uid">The UID of the entity "emitting" the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound. Defaults to using the sound specifier's parameters</param>
    public IPlayingAudioStream? PlayPvs(string filename, EntityUid uid, AudioParams? audioParams = null)
    {
        return Play(filename, Filter.Pvs(uid, entityManager: EntityManager), uid, true, audioParams);
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

    // TODO rename to play static
    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="recipient">The player that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public abstract IPlayingAudioStream? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null);

    // TODO rename to play static
    /// <summary>
    /// Play an audio file at a static position.
    /// </summary>
    /// <param name="sound">The sound specifier that points the audio file(s) that should be played.</param>
    /// <param name="playerFilter">The set of players that will hear the sound.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
    public IPlayingAudioStream? Play(SoundSpecifier? sound, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
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
    public IPlayingAudioStream? PlayStatic(SoundSpecifier? sound, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
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
    public IPlayingAudioStream? PlayStatic(SoundSpecifier? sound, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return sound == null ? null : PlayStatic(GetSound(sound), recipient, coordinates, audioParams ?? sound.Params);
    }

    protected EntityCoordinates GetFallbackCoordinates(MapCoordinates mapCoordinates)
    {
        if (_mapManager.TryFindGridAt(mapCoordinates, out var mapGrid))
            return new EntityCoordinates(mapGrid.GridEntityId, mapGrid.WorldToLocal(mapCoordinates.Position));

        if (_mapManager.HasMapEntity(mapCoordinates.MapId))
            return new EntityCoordinates(_mapManager.GetMapEntityId(mapCoordinates.MapId), mapCoordinates.Position);

        return EntityCoordinates.Invalid;
    }
}
