using System;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class EntitySystemMessage : EntityEventArgs
    {
        /// <summary>
        /// Remote network channel this message came from.
        /// If this is null, the message was raised locally.
        /// </summary>
        [field: NonSerialized]
        public INetChannel NetChannel { get; set; }

        /// <summary>
        /// Entity this message is raised for.
        /// </summary>
        public EntityUid Owner { get; set; }
    }
}
