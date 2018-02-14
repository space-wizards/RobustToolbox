using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     A message containing info to send through the component message system.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class ComponentMessage
    {
        /// <summary>
        ///     Was this message raised from a remote location over the network?
        /// </summary>
        public bool Remote { get; internal set; }

        /// <summary>
        ///     If this is a remote message, should it be sent back to where it came from?
        ///     You want to set this to true if this message is mutable and is being sent over the network.
        /// </summary>
        public bool Reply { get; protected set; }
    }
}
