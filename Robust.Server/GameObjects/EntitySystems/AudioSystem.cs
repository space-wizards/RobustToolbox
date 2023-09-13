using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    [Dependency] private readonly TransformSystem _transform = default!;


    private uint _streamIndex;

    private sealed class AudioSourceServer : IPlayingAudioStream
    {
        public readonly uint Id;
        public uint Identifier => Id;
        public readonly IEnumerable<ICommonSession>? Sessions;
        public AudioParams Parameters;

        private readonly AudioSystem _audioSystem;

        internal AudioSourceServer(
            AudioSystem parent, 
            uint identifier, 
            string filename,
            AudioParams parameters,
            IEnumerable<ICommonSession>? sessions = null)
        {
            _audioSystem = parent;
            Id = identifier;
            Parameters = parameters;
            Sessions = sessions;
        }

        public void Stop()
        {
            _audioSystem.InternalStop(Id, Sessions);
        }
    }

    private void InternalStop(uint id, IEnumerable<ICommonSession>? sessions = null)
    {
        var msg = new StopAudioMessageClient
        {
            Identifier = id
        };

        if (sessions == null)
            RaiseNetworkEvent(msg);
        else
        {
            foreach (var session in sessions)
            {
                RaiseNetworkEvent(msg, session.ConnectedClient);
            }
        }
    }

    private uint CacheIdentifier()
    {
        return unchecked(_streamIndex++);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        var id = CacheIdentifier();
        
        AudioParams parameters = audioParams ?? AudioParams.Default;
        
        var msg = new PlayAudioGlobalMessage
        {
            FileName = filename,
            AudioParams = parameters,
            Identifier = id
        };

        RaiseNetworkEvent(msg, playerFilter, recordReplay);

        return new AudioSourceServer(this, id, filename, parameters, playerFilter.Recipients.ToArray());
    }

    public override IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        if(!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return null;

        var id = CacheIdentifier();

        var fallbackCoordinates = GetFallbackCoordinates(transform.MapPosition);

        AudioParams parameters = audioParams ?? AudioParams.Default;
        
        var msg = new PlayAudioEntityMessage
        {
            FileName = filename,
            Coordinates = GetNetCoordinates(transform.Coordinates),
            FallbackCoordinates = GetNetCoordinates(fallbackCoordinates),
            NetEntity = GetNetEntity(uid),
            AudioParams = audioParams ?? AudioParams.Default,
            Identifier = id,
        };

        RaiseNetworkEvent(msg, playerFilter, recordReplay);

        return new AudioSourceServer(this, id, filename, parameters, playerFilter.Recipients.ToArray());
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        var id = CacheIdentifier();

        var fallbackCoordinates = GetFallbackCoordinates(coordinates.ToMap(EntityManager, _transform));
        
        AudioParams parameters = audioParams ?? AudioParams.Default;

        var msg = new PlayAudioPositionalMessage
        {
            FileName = filename,
            Coordinates = GetNetCoordinates(coordinates),
            FallbackCoordinates = GetNetCoordinates(fallbackCoordinates),
            AudioParams = audioParams ?? AudioParams.Default,
            Identifier = id
        };

        RaiseNetworkEvent(msg, playerFilter, recordReplay);

        return new AudioSourceServer(this, id, filename, parameters, playerFilter.Recipients.ToArray());
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null)
    {
        if (sound == null)
            return null;

        var filter = Filter.Pvs(source, entityManager: EntityManager, playerManager: PlayerManager, cfgManager: CfgManager).RemoveWhereAttachedEntity(e => e == user);
        return Play(sound, filter, source, true, audioParams);
    }

    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user,
        AudioParams? audioParams = null)
    {
        if (sound == null)
            return null;

        var filter = Filter.Pvs(coordinates, entityMan: EntityManager, playerMan: PlayerManager).RemoveWhereAttachedEntity(e => e == user);
        return Play(sound, filter, coordinates, true, audioParams);
    }

    public override IPlayingAudioStream? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, Filter.SinglePlayer(recipient), false, audioParams);
    }

    public override IPlayingAudioStream? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayGlobal(filename, actor.PlayerSession, audioParams);
        return null;
    }

    public override IPlayingAudioStream? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return Play(filename, Filter.SinglePlayer(recipient), uid, false, audioParams);
    }

    public override IPlayingAudioStream? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayEntity(filename, actor.PlayerSession, uid, audioParams);
        return null;
    }

    public override IPlayingAudioStream? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return Play(filename, Filter.SinglePlayer(recipient), coordinates, false, audioParams);
    }

    public override IPlayingAudioStream? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayStatic(filename, actor.PlayerSession, coordinates, audioParams);
        return null;
    }

    
    /// <inheritdoc />
    public override void SetAudioParams(IPlayingAudioStream stream, AudioParams parameters)
    {
        if (!(stream is AudioSourceServer source) || source.Sessions == null)
            return;

        PlayAudioGlobalMessage msg = new PlayAudioGlobalMessage();

        msg.Identifier = source.Id;
        msg.AudioParams = parameters;
        
        Filter filter = Filter.Empty();
        filter.AddPlayers(source.Sessions);
        
        RaiseNetworkEvent(msg, filter);
    }

    /// <inheritdoc />
    public override AudioParams GetAudioParams(IPlayingAudioStream stream)
    {
        if (!(stream is AudioSourceServer source))
            return AudioParams.Default;

        return source.Parameters;
    }
}
