using System;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
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
