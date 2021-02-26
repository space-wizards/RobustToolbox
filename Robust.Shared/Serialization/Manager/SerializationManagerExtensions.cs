#nullable enable
using System;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationManagerExtensions
    {
        public static (T value, DeserializationResult result) ReadWithValueOrThrow<T>(
            this ISerializationManager manager,
            DataNode node,
            ISerializationContext? context = null)
        {
            var result = manager.Read(typeof(T), node, context);

            if (result.RawValue == null)
            {
                throw new NullReferenceException();
            }

            return ((T) result.RawValue, result);
        }

        public static (T value, DeserializationResult result) ReadWithValueOrThrow<T>(
            this ISerializationManager manager,
            Type type,
            DataNode node,
            ISerializationContext? context = null)
        {
            var result = manager.Read(type, node, context);

            if (result.RawValue == null)
            {
                throw new NullReferenceException();
            }

            return ((T) result.RawValue, result);
        }
    }
}
