using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes.DataClasses;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface IServ3Manager
    {
        #region Serialization
        void Initialize();

        T ReadValue<T>(IDataNode node, ISerializationContext? context = null);

        object ReadValue(Type type, IDataNode node, ISerializationContext? context = null);

        IDataNode WriteValue<T>(T value, IDataNodeFactory nodeFactory, bool alwaysWrite = false, ISerializationContext? context = null);

        IDataNode WriteValue(Type type, object value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
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
    }
}
