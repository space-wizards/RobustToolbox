using System;
using SS14.Shared.Players;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class EntitySystemMessage : EntityEventArgs
    {
        /// <summary>
        ///     Entity this message is raised for.
        /// </summary>
        public EntityUid Owner { get; set; }
    }
}
