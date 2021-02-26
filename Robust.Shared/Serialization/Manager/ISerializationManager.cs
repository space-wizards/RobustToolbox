using System;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        #region Serialization

        void Initialize();

        bool HasDataDefinition(Type type);

        int GetDataFieldCount(Type type);

        DeserializationResult PopulateDataDefinition<T>(DeserializedFieldEntry[] fields) where T : new();

        DeserializationResult Read<T>(DataNode node, ISerializationContext? context = null);

        DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null);

        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null);

        // TODO Paul move these to SerializationExtensions?
        T? ReadValue<T>(Type type, DataNode node, ISerializationContext? context = null);

        T? ReadValue<T>(DataNode node, ISerializationContext? context = null);

        T ReadValueOrThrow<T>(DataNode node, ISerializationContext? context = null);

        T ReadValueOrThrow<T>(Type type, DataNode node, ISerializationContext? context = null);

        object ReadValueOrThrow(Type type, DataNode node, ISerializationContext? context = null);

        (DeserializationResult result, object? value) ReadWithValue(Type type, DataNode node, ISerializationContext? context = null);

        (T? value, DeserializationResult result) ReadWithValue<T>(Type type, DataNode node,
            ISerializationContext? context = null);

        T PushInheritance<T>(
            DeserializationResult from,
            T value,
            DeserializationResult valueResult)
            where T : notnull;

        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
            where T : notnull;

        TNode WriteValueAs<TNode>(object value, bool alwaysWrite = false, ISerializationContext? context = null)
            where TNode : DataNode;

        DataNode WriteValue(Type type, object value, bool alwaysWrite = false,
            ISerializationContext? context = null);

        object? Copy(object? source, object? target);

        T? Copy<T>(object? source, T? target);

        object? CreateCopy(object? source);

        T? CreateCopy<T>(T? source);

        #endregion

        #region Flags And Constants

        int ReadFlag(Type tagType, DataNode node);
        int ReadConstant(Type tagType, DataNode node);

        DataNode WriteFlag(Type tagType, int flag);
        DataNode WriteConstant(Type tagType, int constant);

        #endregion
    }
}
