using System;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        #region Serialization
        void Initialize();

        DeserializationResult ReadValue<T>(DataNode node, ISerializationContext? context = null);

        DeserializationResult ReadValue(Type type, DataNode node, ISerializationContext? context = null);

        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
            where T : notnull;

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
