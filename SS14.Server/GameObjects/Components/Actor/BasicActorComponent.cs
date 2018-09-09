using System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.ViewVariables;

namespace SS14.Server.GameObjects
{
    public class BasicActorComponent : Component, IActorComponent
    {
        public override string Name => "BasicActor";

        [ViewVariables]
        public IPlayerSession playerSession { get; internal set; }
    }

    /// <summary>
    ///     Raised on an entity whenever a player attaches to this entity.
    /// </summary>
    [Serializable]
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
    public class PlayerDetachedMsg : ComponentMessage
    {
        public IPlayerSession OldPlayer { get; }

        public PlayerDetachedMsg(IPlayerSession oldPlayer)
        {
            OldPlayer = oldPlayer;
        }
    }
}
