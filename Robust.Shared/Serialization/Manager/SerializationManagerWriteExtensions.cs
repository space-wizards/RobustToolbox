using System;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationManagerWriteExtensions
    {
        public static T WriteValueAs<T>(
            this ISerializationManager manager,
            object value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
            where T : DataNode
        {
            return manager.WriteValueAs<T>(value.GetType(), value, alwaysWrite, context);
        }

        public static T WriteValueAs<T>(
            this ISerializationManager manager,
            Type type,
            object value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
            where T : DataNode
        {
            return (T) manager.WriteValue(type, value, alwaysWrite, context);
        }
    }
}
