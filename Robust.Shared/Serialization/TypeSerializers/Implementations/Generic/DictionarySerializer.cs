using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic
{
    [TypeSerializer]
    public class DictionarySerializer<TKey, TValue> :
        ITypeSerializer<Dictionary<TKey, TValue>, MappingDataNode>,
        ITypeSerializer<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>,
        ITypeSerializer<SortedDictionary<TKey, TValue>, MappingDataNode> where TKey : notnull
    {
        private MappingDataNode InterfaceWrite(
            ISerializationManager serializationManager,
            IDictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mappingNode = new MappingDataNode();

            foreach (var (key, val) in value)
            {
                mappingNode.Add(
                    serializationManager.WriteValue(key, alwaysWrite, context),
                    serializationManager.WriteValue(typeof(TValue), val, alwaysWrite, context));
            }

            return mappingNode;
        }

        public DeserializationResult Read(ISerializationManager serializationManager,
            MappingDataNode node, IDependencyCollection dependencies, bool skipHook, ISerializationContext? context)
        {
            var dict = new Dictionary<TKey, TValue>();
            var mappedFields = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (key, value) in node.Children)
            {
                var (keyVal, keyResult) = serializationManager.ReadWithValueOrThrow<TKey>(key, context, skipHook);
                var (valueResult, valueVal) = serializationManager.ReadWithValueCast<TValue>(typeof(TValue), value, context, skipHook);

                dict.Add(keyVal, valueVal!);
                mappedFields.Add(keyResult, valueResult);
            }

            return new DeserializedDictionary<Dictionary<TKey, TValue>, TKey, TValue>(dict, mappedFields, dictInstance => dictInstance);
        }

        ValidationNode ITypeValidator<SortedDictionary<TKey, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<Dictionary<TKey, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager,
            MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node, ISerializationContext? context)
        {
            var mapping = new Dictionary<ValidationNode, ValidationNode>();
            foreach (var (key, val) in node.Children)
            {
                mapping.Add(serializationManager.ValidateNode(typeof(TKey), key, context), serializationManager.ValidateNode(typeof(TValue), val, context));
            }

            return new ValidatedMappingNode(mapping);
        }

        public DataNode Write(ISerializationManager serializationManager, Dictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, SortedDictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyDictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(serializationManager, value.ToDictionary(k => k.Key, v => v.Value), alwaysWrite, context);
        }

        DeserializationResult
            ITypeReader<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Read(
                ISerializationManager serializationManager, MappingDataNode node,
                IDependencyCollection dependencies,
                bool skipHook, ISerializationContext? context)
        {
            var dict = new Dictionary<TKey, TValue>();
            var mappedFields = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (key, value) in node.Children)
            {
                var (keyVal, keyResult) = serializationManager.ReadWithValueOrThrow<TKey>(key, context, skipHook);
                var (valueResult, valueVal) = serializationManager.ReadWithValueCast<TValue>(typeof(TValue), value, context, skipHook);

                dict.Add(keyVal, valueVal!);
                mappedFields.Add(keyResult, valueResult);
            }

            return new DeserializedDictionary<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(dict, mappedFields, dictInstance => dictInstance);
        }

        DeserializationResult
            ITypeReader<SortedDictionary<TKey, TValue>, MappingDataNode>.Read(
                ISerializationManager serializationManager, MappingDataNode node,
                IDependencyCollection dependencies,
                bool skipHook, ISerializationContext? context)
        {
            var dict = new SortedDictionary<TKey, TValue>();
            var mappedFields = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (key, value) in node.Children)
            {
                var (keyVal, keyResult) = serializationManager.ReadWithValueOrThrow<TKey>(key, context, skipHook);
                var (valueResult, valueVal) = serializationManager.ReadWithValueCast<TValue>(typeof(TValue), value, context, skipHook);

                dict.Add(keyVal, valueVal!);
                mappedFields.Add(keyResult, valueResult);
            }

            return new DeserializedDictionary<SortedDictionary<TKey, TValue>, TKey, TValue>(dict, mappedFields, dictInstance => new SortedDictionary<TKey, TValue>(dictInstance));
        }

        [MustUseReturnValue]
        private T CopyInternal<T>(ISerializationManager serializationManager, IReadOnlyDictionary<TKey, TValue> source, T target, ISerializationContext? context) where T : IDictionary<TKey, TValue>
        {
            target.Clear();

            foreach (var (key, value) in source)
            {
                var keyCopy = serializationManager.CreateCopy(key, context) ?? throw new NullReferenceException();
                var valueCopy = serializationManager.CreateCopy(value, context)!;

                target.Add(keyCopy, valueCopy);
            }

            return target;
        }

        [MustUseReturnValue]
        public Dictionary<TKey, TValue> Copy(ISerializationManager serializationManager,
            Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> target,
            bool skipHook, ISerializationContext? context = null)
        {
            return CopyInternal(serializationManager, source, target, context);
        }

        [MustUseReturnValue]
        public IReadOnlyDictionary<TKey, TValue> Copy(ISerializationManager serializationManager,
            IReadOnlyDictionary<TKey, TValue> source, IReadOnlyDictionary<TKey, TValue> target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            if (target is Dictionary<TKey, TValue> targetDictionary)
            {
                return CopyInternal(serializationManager, source, targetDictionary, context);
            }

            var dictionary = new Dictionary<TKey, TValue>(source.Count);

            foreach (var (key, value) in source)
            {
                var keyCopy = serializationManager.CreateCopy(key, context) ?? throw new NullReferenceException();
                var valueCopy = serializationManager.CreateCopy(value, context)!;

                dictionary.Add(keyCopy, valueCopy);
            }

            return dictionary;
        }

        [MustUseReturnValue]
        public SortedDictionary<TKey, TValue> Copy(ISerializationManager serializationManager,
            SortedDictionary<TKey, TValue> source, SortedDictionary<TKey, TValue> target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return CopyInternal(serializationManager, source, target, context);
        }
    }
}
