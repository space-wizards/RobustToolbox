using System;

namespace Robust.Shared.Serialization.Manager.Attributes.Deserializer
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Struct |
        AttributeTargets.Enum)]
    public class DataFieldDeserializerAttribute : Attribute
    {
        public DataFieldDeserializerAttribute(Type deserializer)
        {
            if (!deserializer.IsAssignableTo(typeof(IDataFieldDeserializer)))
            {
                throw new ArgumentException($"Type {deserializer} does not implement {nameof(IDataFieldDeserializer)}");
            }

            Deserializer = deserializer;
        }

        public Type Deserializer { get; }
    }
}
