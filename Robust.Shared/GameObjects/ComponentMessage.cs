using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     A message containing info to send through the component message system.
    /// </summary>
    [Serializable, NetSerializable]
    [Obsolete("Component messages are deprecated. Use directed local events instead.")]
    public abstract class ComponentMessage
    {
        /// <summary>
        ///     Was this message raised from a remote location over the network?
        /// </summary>
        public bool Remote { get; internal set; }

        /// <summary>
        ///     If this is a remote message, will it only be sent to the corresponding component?
        ///     If this is not a remote message, this flag does nothing.
        /// </summary>
        public bool Directed { get; protected set; }
    }
}
