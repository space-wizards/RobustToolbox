using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     System that handles players being attached/detached from entities.
    /// </summary>
    [UsedImplicitly]
    public sealed class ActorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AttachPlayerEvent>(OnActorPlayerAttach);
            SubscribeLocalEvent<ActorComponent, DetachPlayerEvent>(OnActorPlayerDetach);
            SubscribeLocalEvent<ActorComponent, ComponentShutdown>(OnActorShutdown);
        }

        private void OnActorPlayerAttach(AttachPlayerEvent args)
        {
            args.Result = Attach(args.Uid, args.Player, args.Force, out var forceKicked);
            args.ForceKicked = forceKicked;
        }

        /// <summary>
        ///     Attaches a player session to an entity, optionally kicking any sessions already attached to it.
        /// </summary>
        /// <param name="uid">The entity to attach the player to</param>
        /// <param name="player">The player to attach to the entity</param>
        /// <param name="force">Whether to kick any existing players from the entity</param>
        /// <returns>Whether the attach succeeded, or not.</returns>
        public bool Attach(EntityUid uid, IPlayerSession player, bool force = false)
        {
            return Attach(uid, player, false, out _);
        }

        /// <summary>
        ///     Attaches a player session to an entity, optionally kicking any sessions already attached to it.
        /// </summary>
        /// <param name="uid">The entity to attach the player to</param>
        /// <param name="player">The player to attach to the entity</param>
        /// <param name="force">Whether to kick any existing players from the entity</param>
        /// <param name="forceKicked">The player that was forcefully kicked, or null.</param>
        /// <returns>Whether the attach succeeded, or not.</returns>
        public bool Attach(EntityUid uid, IPlayerSession player, bool force, out IPlayerSession? forceKicked)
        {
            // Null by default.
            forceKicked = null;

            // Cannot attach to a deleted/nonexisting entity.
            if (!EntityManager.EntityExists(uid))
            {
                return false;
            }

            // Check if there was a player attached to the entity already...
            if (EntityManager.TryGetComponent(uid, out ActorComponent actor))
            {
                // If we're not forcing the attach, this fails.
                if (!force)
                {
                    return false;
                }

                // Set the event's force-kicked session before detaching it.
                forceKicked = actor.PlayerSession;

                // This detach cannot fail, as a player is attached to this entity.
                // It's important to note that detaching the player removes the component.
                RaiseLocalEvent(uid, new DetachPlayerEvent());
            }

            // We add the actor component.
            actor = EntityManager.AddComponent<ActorComponent>(uid);
            actor.PlayerSession = player;
            player.SetAttachedEntity(actor.Owner);

            // The player is fully attached now, raise an event!
            RaiseLocalEvent(uid, new PlayerAttachedEvent(actor.Owner, player, forceKicked));
            return true;
        }

        // Not gonna make this method call Detach as all we have to do is remove a component...
        private void OnActorPlayerDetach(EntityUid uid, ActorComponent component, DetachPlayerEvent args)
        {
            // Removing the component will call shutdown, and our subscription will handle the rest of the detach logic.
            EntityManager.RemoveComponent<ActorComponent>(uid);
            args.Result = true;
        }

        /// <summary>
        ///     Detaches an attached session from the entity, if any.
        /// </summary>
        /// <param name="entity">The entity player sessions will be detached from.</param>
        /// <returns>Whether any player session was detached.</returns>
        public bool Detach(EntityUid entity)
        {
            if (!EntityManager.HasComponent<ActorComponent>(entity))
                return false;

            // Removing the component will call shutdown, and our subscription will handle the rest of the detach logic.
            EntityManager.RemoveComponent<ActorComponent>(entity);
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
            return uid == default || Detach(uid);
        }

        private void OnActorShutdown(EntityUid entity, ActorComponent component, ComponentShutdown args)
        {
            component.PlayerSession.SetAttachedEntity(default);

            // The player is fully detached now that the component has shut down.
            RaiseLocalEvent(entity, new PlayerDetachedEvent(entity, component.PlayerSession));
        }
    }

    /// <summary>
    ///     Raise this broadcast event to attach a player to an entity, optionally detaching the player attached to it.
    /// </summary>
    public sealed class AttachPlayerEvent : EntityEventArgs
    {
        /// <summary>
        ///     Player to attach to the entity.
        ///     Input parameter.
        /// </summary>
        public IPlayerSession Player { get; }

        /// <summary>
        ///     Entity to attach the player to.
        ///     Input parameter.
        /// </summary>
        public EntityUid Uid { get; }

        /// <summary>
        ///     Whether to force-attach the player,
        ///     detaching any players attached to it if any.
        ///     Input parameter.
        /// </summary>
        public bool Force { get; }

        /// <summary>
        ///     If the attach was forced and there was a player attached to the entity before, this will be it.
        ///     Output parameter.
        /// </summary>
        public IPlayerSession? ForceKicked { get; set; }

        /// <summary>
        ///     Whether the player was attached correctly.
        ///     False if not forcing and the entity already had a player attached to it.
        ///     Output parameter.
        /// </summary>
        public bool Result { get; set; } = false;

        public AttachPlayerEvent(EntityUid uid, IPlayerSession player, bool force = false)
        {
            Uid = uid;
            Player = player;
            Force = force;
        }
    }

    /// <summary>
    ///     Raise this directed event to detach a player from an entity.
    /// </summary>
    public sealed class DetachPlayerEvent : EntityEventArgs
    {
        /// <summary>
        ///     Whether the player was detached correctly.
        ///     Fails if no player was attached to the entity.
        ///     Output parameter.
        /// </summary>
        public bool Result { get; set; } = false;
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
