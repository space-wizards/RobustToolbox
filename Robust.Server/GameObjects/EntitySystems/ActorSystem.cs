using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     System that handles players being attached/detached from entities.
    /// </summary>
    [UsedImplicitly]
    public sealed class ActorSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActorComponent, ComponentShutdown>(OnActorShutdown);
        }

        /// <summary>
        ///     Attaches a player session to an entity, optionally kicking any sessions already attached to it.
        /// </summary>
        /// <param name="uid">The entity to attach the player to</param>
        /// <param name="player">The player to attach to the entity</param>
        /// <param name="force">Whether to kick any existing players from the entity</param>
        /// <returns>Whether the attach succeeded, or not.</returns>
        public bool Attach(EntityUid? uid, IPlayerSession player, bool force = false)
        {
            return Attach(uid, player, false, out _);
        }

        /// <summary>
        ///     Attaches a player session to an entity, optionally kicking any sessions already attached to it.
        /// </summary>
        /// <param name="entity">The entity to attach the player to</param>
        /// <param name="player">The player to attach to the entity</param>
        /// <param name="force">Whether to kick any existing players from the entity</param>
        /// <param name="forceKicked">The player that was forcefully kicked, or null.</param>
        /// <returns>Whether the attach succeeded, or not.</returns>
        public bool Attach(EntityUid? entity, IPlayerSession player, bool force, out IPlayerSession? forceKicked)
        {
            // Null by default.
            forceKicked = null;

            if (player.AttachedEntity == entity)
                return true;

            if (entity is not { } uid)
                return Detach(player);

            // Cannot attach to a deleted, nonexisting or terminating entity.
            if (!TryComp(uid, out MetaDataComponent? meta) || meta.EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return false;
            }

            // Check if there was a player attached to the entity already...
            if (EntityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                // If we're not forcing the attach, this fails.
                if (!force)
                    return false;

                // Set the event's force-kicked session before detaching it.
                forceKicked = actor.PlayerSession;
                Detach(uid, actor);
            }

            // Detach from the currently attached entity.
            if (!Detach(player))
                return false;

            // We add the actor component.
            actor = EntityManager.AddComponent<ActorComponent>(uid);
            EntityManager.EnsureComponent<EyeComponent>(uid);
            actor.PlayerSession = player;
            player.SetAttachedEntity(uid);

            // The player is fully attached now, raise an event!
            RaiseLocalEvent(uid, new PlayerAttachedEvent(uid, player, forceKicked), true);
            DebugTools.Assert(player.AttachedEntity == entity);
            return true;
        }

        /// <summary>
        ///     Detaches an attached session from the entity, if any.
        /// </summary>
        /// <param name="entity">The entity player sessions will be detached from.</param>
        /// <returns>Whether any player session was detached.</returns>
        public bool Detach(EntityUid uid, ActorComponent? actor = null)
        {
            if (!Resolve(uid, ref actor, false))
                return false;

            RemComp(uid, actor);
            return true;
        }

        /// <summary>
        ///     Detaches this player from its attached entity, if any.
        /// </summary>
        /// <param name="player">The player session that will be detached from any attached entities.</param>
        /// <returns>Whether the player is now detached from any entities.
        /// This returns true if the player wasn't attached to any entity.</returns>
        public bool Detach(IPlayerSession player)
        {
            var uid = player.AttachedEntity;
            return uid == null || Detach(uid.Value);
        }

        private void OnActorShutdown(EntityUid entity, ActorComponent component, ComponentShutdown args)
        {
            component.PlayerSession.SetAttachedEntity(null);

            // The player is fully detached now that the component has shut down.
            RaiseLocalEvent(entity, new PlayerDetachedEvent(entity, component.PlayerSession), true);
        }

        public bool TryGetActorFromUserId(NetUserId? userId, [NotNullWhen(true)] out IPlayerSession? actor, [MaybeNullWhen(true)] out EntityUid? actorEntity)
        {
            actor = null;
            actorEntity = null;
            if (userId != null)
            {
                if (!_playerManager.TryGetSessionById(userId.Value, out actor))
                    return false;
                actorEntity = actor.AttachedEntity;
            }

            return actor != null;
        }
    }

    /// <summary>
    ///     Event for when a player has been attached to an entity.
    /// </summary>
    public sealed class PlayerAttachedEvent : EntityEventArgs
    {
        public EntityUid Entity { get; }
        public IPlayerSession Player { get; }

        /// <summary>
        ///     The player session that was forcefully kicked from the entity, if any.
        /// </summary>
        public IPlayerSession? Kicked { get; }

        public PlayerAttachedEvent(EntityUid entity, IPlayerSession player, IPlayerSession? kicked = null)
        {
            Entity = entity;
            Player = player;
            Kicked = kicked;
        }
    }

    /// <summary>
    ///     Event for when a player has been detached from an entity.
    /// </summary>
    public sealed class PlayerDetachedEvent : EntityEventArgs
    {
        public EntityUid Entity { get; }
        public IPlayerSession Player { get; }

        public PlayerDetachedEvent(EntityUid entity, IPlayerSession player)
        {
            Entity = entity;
            Player = player;
        }
    }
}
