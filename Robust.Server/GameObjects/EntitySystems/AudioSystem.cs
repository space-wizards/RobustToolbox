using System.Numerics;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects;

[UsedImplicitly]
public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    private void AddAudioFilter(EntityUid uid, Filter filter)
    {
        var nent = GetNetEntity(uid);
        _pvs.AddSessionOverrides(nent, filter);
    }

    private void SetupAudio(AudioComponent component, string fileName, AudioType audioType, AudioParams? audioParams)
    {
        audioParams ??= AudioParams.Default;
        component.FileName = fileName;
        component.Params = audioParams.Value;
        component.AudioType = audioType;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        var entity = Spawn(AudioEntity, MapCoordinates.Nullspace);
        var audio = Comp<AudioComponent>(entity);
        SetupAudio(audio, filename, AudioType.Global, audioParams);
        AddAudioFilter(entity, playerFilter);

        return (entity, audio.Stream);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        if (!Exists(uid))
            return null;

        var entity = Spawn(AudioEntity, new EntityCoordinates(uid, Vector2.Zero));
        var audio = Comp<AudioComponent>(entity);
        SetupAudio(audio, filename, AudioType.Local, audioParams);
        AddAudioFilter(entity, playerFilter);

        return (entity, audio.Stream);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        if (!coordinates.IsValid(EntityManager))
            return null;

        var entity = Spawn(AudioEntity, coordinates);
        var audio = Comp<AudioComponent>(entity);
        SetupAudio(audio, filename, AudioType.Local, audioParams);
        AddAudioFilter(entity, playerFilter);

        return (entity, audio.Stream);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user)
    {
        if (sound == null)
            return null;

        var filter = Filter.Pvs(source, entityManager: EntityManager, playerManager: PlayerManager, cfgManager: CfgManager).RemoveWhereAttachedEntity(e => e == user);
        return PlayEntity(GetSound(sound), filter, source, true, sound.Params);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user)
    {
        if (sound == null)
            return null;

        var filter = Filter.Pvs(coordinates, entityMan: EntityManager, playerMan: PlayerManager).RemoveWhereAttachedEntity(e => e == user);
        return PlayStatic(GetSound(sound), filter, coordinates, true, sound.Params);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, Filter.SinglePlayer(recipient), false, audioParams);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayGlobal(filename, actor.PlayerSession, audioParams);

        return null;
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, Filter.SinglePlayer(recipient), uid, false, audioParams);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayEntity(filename, actor.PlayerSession, uid, audioParams);

        return null;
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, Filter.SinglePlayer(recipient), coordinates, false, audioParams);
    }

    public override (EntityUid Entity, IPlayingAudioStream Stream)? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayStatic(filename, actor.PlayerSession, coordinates, audioParams);

        return null;
    }
}
