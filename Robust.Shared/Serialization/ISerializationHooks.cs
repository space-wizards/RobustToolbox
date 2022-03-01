using System;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Provides a method that gets executed after deserialization is complete and a method that gets executed before serialization
    /// </summary>
    [RequiresExplicitImplementation]
    [Obsolete($"Avoid using ISerializationHooks in favour of (Custom)TypeSerializers or ComponentInit-Events.")]
    public interface ISerializationHooks
    {
        /// <summary>
        /// Gets executed after deserialization is complete
        /// </summary>
        void AfterDeserialization() {}

        /// <summary>
        /// Gets executed before serialization
        /// </summary>
        void BeforeSerialization() {}
    }
}
