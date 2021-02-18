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

        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null) where T : notnull;

        DataNode WriteValue(Type type, object value, bool alwaysWrite = false,
            ISerializationContext? context = null);

        object Copy(object source, object target);
        object PushInheritance(object source, object target);

        #endregion

        #region DataClasses
        /// <summary>
        /// Returns an empty dataclass for type <paramref name="classType"/>
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        DataClass? GetEmptyDataClass(Type classType);

        /// <summary>
        /// Returns an empty dataclass for <paramref name="compName"/>
        /// </summary>
        /// <param name="compName"></param>
        /// <returns></returns>
        DataClass GetEmptyComponentDataClass(string compName);

        Type GetDataClassType(Type classType);

        Type GetComponentDataClassType(string compName);

        object DataClass2Object(DataClass dataClass, object obj);

        void Object2DataClass(object obj, DataClass dataClass);

        public bool TryGetDataClassField<T>(DataClass dataClass, string name, [NotNullWhen(true)] out T? value);

        public static string GetAutoDataClassMetadataName(Type type)
        {
            return $"{type.Namespace}.{type.Name}_AUTODATA";
        }
        #endregion

        #region Flags And Constants

        int ReadFlag(Type tagType, DataNode node);
        int ReadConstant(Type tagType, DataNode node);

        DataNode WriteFlag(Type tagType, int flag);
        DataNode WriteConstant(Type tagType, int constant);

        #endregion
    }
}
