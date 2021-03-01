using System;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        #region Serialization

        void Initialize();

        bool HasDataDefinition(Type type);

        ValidatedNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null);

        DeserializationResult CreateDataDefinition<T>(DeserializedFieldEntry[] fields) where T : notnull, new();

        DeserializationResult PopulateDataDefinition<T>(T obj, DeserializedDefinition<T> definition) where T : notnull, new();

        DeserializationResult PopulateDataDefinition(object obj, IDeserializedDefinition deserializationResult);

        DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null);

        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null);

        T? ReadValue<T>(Type type, DataNode node, ISerializationContext? context = null);

        T? ReadValue<T>(DataNode node, ISerializationContext? context = null);

        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
            where T : notnull;

        DataNode WriteValue(Type type, object value, bool alwaysWrite = false, ISerializationContext? context = null);

        object? Copy(object? source, object? target, ISerializationContext? context = null);

        T? Copy<T>(object? source, T? target, ISerializationContext? context = null);

        object? CreateCopy(object? source, ISerializationContext? context = null);

        T? CreateCopy<T>(T? source, ISerializationContext? context = null);

        #endregion

        #region Flags And Constants

        int ReadFlag(Type tagType, DataNode node);
        int ReadConstant(Type tagType, DataNode node);

        DataNode WriteFlag(Type tagType, int flag);
        DataNode WriteConstant(Type tagType, int constant);

        #endregion
    }
}
