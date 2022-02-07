using System;

namespace Robust.Shared.Serialization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SerializedTypeAttribute : Attribute
    {
        /// <summary>
        ///     Name of this type in serialized files.
        /// </summary>
        public string SerializeName { get; }

        public SerializedTypeAttribute(string serializeName)
        {
            SerializeName = serializeName;
        }
    }
}
