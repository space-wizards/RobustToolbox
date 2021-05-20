using System;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class ActorComponent : Component
    {
        public override string Name => "Actor";

        [ViewVariables] public IPlayerSession PlayerSession { get; internal set; } = default!;

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            // Warning: careful here, Detach removes this component, make sure this is after the base shutdown
            // to prevent infinite recursion
            // ReSharper disable once ConstantConditionalAccessQualifier
            PlayerSession?.DetachFromEntity();
        }
    }

    /// <summary>
    ///     Raised on an entity whenever a player attaches to this entity.
    /// </summary>
    [Serializable]
    [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
    public class PlayerAttachedMsg : ComponentMessage
    {
        public IPlayerSession NewPlayer { get; }

        public PlayerAttachedMsg(IPlayerSession newPlayer)
        {
            NewPlayer = newPlayer;
        }
    }

    /// <summary>
    ///     Raised on an entity whenever a player detaches from this entity.
    /// </summary>
    [Serializable]
    [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
    public class PlayerDetachedMsg : ComponentMessage
    {
        public IPlayerSession OldPlayer { get; }

        public PlayerDetachedMsg(IPlayerSession oldPlayer)
        {
            OldPlayer = oldPlayer;
        }
    }

    public class PlayerAttachSystemMessage : EntityEventArgs
    {
        public PlayerAttachSystemMessage(IEntity entity, IPlayerSession newPlayer)
        {
            Entity = entity;
            NewPlayer = newPlayer;
        }

        public IEntity Entity { get; }
        public IPlayerSession NewPlayer { get; }
    }

    public class PlayerDetachedSystemMessage : EntityEventArgs
    {
        public PlayerDetachedSystemMessage(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
