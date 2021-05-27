using System;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class ActorComponent : Component
    {
        public override string Name => "Actor";

        [ViewVariables]
        public IPlayerSession PlayerSession { get; internal set; } = default!;
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
}
