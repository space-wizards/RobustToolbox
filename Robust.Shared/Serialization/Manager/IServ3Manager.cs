using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface IServ3Manager
    {
        #region Serialization
        void Initialize();

        T ReadValue<T>(DataNode node, ISerializationContext? context = null);

        object ReadValue(Type type, DataNode node, ISerializationContext? context = null);

        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null);

        DataNode WriteValue(Type type, object value, bool alwaysWrite = false,
            ISerializationContext? context = null);

        object? Copy(object? source, object? target);
        object? CreateCopy(object? source);
        object PushInheritance(object source, object target);
        #endregion

        #region Flags And Constants

        int ReadFlag(Type tagType, DataNode node);
        int ReadConstant(Type tagType, DataNode node);

        DataNode WriteFlag(Type tagType, int flag);
        DataNode WriteConstant(Type tagType, int constant);

        #endregion
    }
}
