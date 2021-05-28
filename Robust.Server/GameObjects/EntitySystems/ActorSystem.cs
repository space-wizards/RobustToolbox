using System;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     System that handles players being attached/detached from entities.
    /// </summary>
    [UsedImplicitly]
    public class ActorSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AttachPlayerEvent>(OnActorPlayerAttach);
            SubscribeLocalEvent<ActorComponent, DetachPlayerEvent>(OnActorPlayerDetach);
            SubscribeLocalEvent<ActorComponent, ComponentShutdown>(OnActorShutdown);
        }

        private void OnActorPlayerAttach(AttachPlayerEvent args)
        {
            // Cannot attach to a deleted entity.
            if (args.Entity.Deleted)
            {
                args.Result = false;
                return;
            }

            var uid = args.Entity.Uid;

            // Check if there was a player attached to the entity already...
            if (ComponentManager.TryGetComponent(uid, out ActorComponent actor))
            {
                // If we're not forcing the attach, this fails.
                if (!args.Force)
                {
                    args.Result = false;
                    return;
                }

                // Set the event's force-kicked session before detaching it.
                args.ForceKicked = actor.PlayerSession;

                // This detach cannot fail, as a player is attached to this entity.
                // It's important to note that detaching the player removes the component.
                RaiseLocalEvent(uid, new DetachPlayerEvent());
            }

            // We add the actor component.
            actor = ComponentManager.AddComponent<ActorComponent>(args.Entity);
            actor.PlayerSession = args.Player;
            args.Player.SetAttachedEntity(args.Entity);
            args.Result = true;

            // TODO: Remove component message.
            args.Entity.SendMessage(actor, new PlayerAttachedMsg(args.Player));

            // The player is fully attached now, raise an event!
            RaiseLocalEvent(uid, new PlayerAttachedEvent(args.Entity, args.Player));
        }

        private void OnActorPlayerDetach(EntityUid uid, ActorComponent component, DetachPlayerEvent args)
        {
            // Removing the component will call shutdown, and our subscription will handle the rest of the detach logic.
            ComponentManager.RemoveComponent<ActorComponent>(uid);
            args.Result = true;
        }

        private void OnActorShutdown(EntityUid uid, ActorComponent component, ComponentShutdown args)
        {
            component.PlayerSession.SetAttachedEntity(null);

            var entity = EntityManager.GetEntity(uid);

            // TODO: Remove component message.
            entity.SendMessage(component, new PlayerDetachedMsg(component.PlayerSession));

            // The player is fully detached now that the component has shut down.
            RaiseLocalEvent(uid, new PlayerDetachedEvent(entity, component.PlayerSession));
        }
    }

    /// <summary>
    ///     Raise this broadcast event to attach a player to an entity, optionally detaching the player attached to it.
    /// </summary>
    public class AttachPlayerEvent : EntityEventArgs
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
        public IEntity Entity { get; }

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

        public AttachPlayerEvent(IEntity entity, IPlayerSession player, bool force = false)
        {
            Entity = entity;
            Player = player;
            Force = force;
        }
    }

    /// <summary>
    ///     Raise this directed event to detach a player from an entity.
    /// </summary>
    public class DetachPlayerEvent : EntityEventArgs
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
    public class PlayerAttachedEvent : EntityEventArgs
    {
        public IEntity Entity { get; }
        public IPlayerSession Player { get; }

        public PlayerAttachedEvent(IEntity entity, IPlayerSession player)
        {
            Entity = entity;
            Player = player;
        }
    }

    /// <summary>
    ///     Event for when a player has been detached from an entity.
    /// </summary>
    public class PlayerDetachedEvent : EntityEventArgs
    {
        public IEntity Entity { get; }
        public IPlayerSession Player { get; }

        public PlayerDetachedEvent(IEntity entity, IPlayerSession player)
        {
            Entity = entity;
            Player = player;
        }
    }
}
