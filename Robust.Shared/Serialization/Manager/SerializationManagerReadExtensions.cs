#nullable enable
using System;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationManagerReadExtensions
    {
        public static T ReadValueOrThrow<T>(
            this ISerializationManager manager,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            return manager.ReadValue<T>(node, context, skipHook) ?? throw new NullReferenceException();
        }

        public static T ReadValueOrThrow<T>(
            this ISerializationManager manager,
            Type type,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            return manager.ReadValueCast<T>(type, node, context, skipHook) ?? throw new NullReferenceException();
        }

        public static object ReadValueOrThrow(
            this ISerializationManager manager,
            Type type,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            return manager.ReadValue(type, node, context, skipHook) ?? throw new NullReferenceException();
        }

        public static (DeserializationResult result, object? value) ReadWithValue(
            this ISerializationManager manager,
            Type type, DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var result = manager.Read(type, node, context, skipHook);
            return (result, result.RawValue);
        }

        public static (DeserializationResult result, T? value) ReadWithValue<T>(
            this ISerializationManager manager,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var result = manager.Read(typeof(T), node, context, skipHook);

            if (result.RawValue == null)
            {
                return (result, default);
            }

            return (result, (T) result.RawValue);
        }

        public static (DeserializationResult result, T? value) ReadWithValueCast<T>(
            this ISerializationManager manager,
            Type type,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var result = manager.Read(type, node, context, skipHook);

            if (result.RawValue == null)
            {
                return (result, default);
            }

            return (result, (T) result.RawValue);
        }


        public static (T value, DeserializationResult result) ReadWithValueOrThrow<T>(
            this ISerializationManager manager,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var result = manager.Read(typeof(T), node, context, skipHook);

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
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var result = manager.Read(type, node, context, skipHook);

            if (result.RawValue == null)
            {
                throw new NullReferenceException();
            }

            return ((T) result.RawValue, result);
        }
    }
}
