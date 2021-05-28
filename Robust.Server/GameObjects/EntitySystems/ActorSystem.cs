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
            SubscribeLocalEvent<ActorComponent, ComponentShutdown>(OnActorShutdown);
            SubscribeLocalEvent<ActorComponent, DetachPlayerEvent>(OnActorPlayerDetach);
            SubscribeLocalEvent<AttachPlayerEvent>(OnActorPlayerAttach);
        }

        private void OnActorPlayerAttach(AttachPlayerEvent args)
        {
            if (args.Entity.Deleted)
                return;

            var uid = args.Entity.Uid;

            if (ComponentManager.TryGetComponent(uid, out ActorComponent actor))
            {
                if (!args.Force)
                {
                    args.Result = false;
                    return;
                }

                // This cannot fail, as a player is attached to this entity.
                RaiseLocalEvent(uid, new DetachPlayerEvent());
            }

            actor = ComponentManager.AddComponent<ActorComponent>(args.Entity);
            actor.PlayerSession = args.Player;
            args.Player.SetAttachedEntity(args.Entity);
            args.Result = true;

            args.Entity.SendMessage(actor, new PlayerAttachedMsg(args.Player));
            RaiseLocalEvent(uid, new PlayerAttachedEvent(args.Entity, args.Player));
        }

        private void OnActorPlayerDetach(EntityUid uid, ActorComponent component, DetachPlayerEvent args)
        {
            EntityManager.GetEntity(uid).SendMessage(component, new PlayerDetachedMsg(component.PlayerSession));
            ComponentManager.RemoveComponent<ActorComponent>(uid);
            args.Result = true;
        }

        private void OnActorShutdown(EntityUid uid, ActorComponent component, ComponentShutdown args)
        {
            component.PlayerSession.SetAttachedEntity(null);

            // The player is only fully detached when the component shuts down.
            RaiseLocalEvent(uid, new PlayerDetachedEvent(EntityManager.GetEntity(uid), component.PlayerSession));
        }
    }

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

    public class DetachPlayerEvent : EntityEventArgs
    {
        /// <summary>
        ///     Whether the player was detached correctly.
        ///     Fails if no player was attached to the entity.
        ///     Output parameter.
        /// </summary>
        public bool Result { get; set; } = false;
    }

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
