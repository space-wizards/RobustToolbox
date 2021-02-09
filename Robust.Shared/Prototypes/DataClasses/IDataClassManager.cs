using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes.DataClasses;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public interface IDataClassManager
    {
        void Initialize();

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

        void PopulateObject(object obj, DataClass dataClass);

        void PopulateDataClass(object obj, DataClass dataClass);

        public bool TryGetDataClassField<T>(DataClass dataClass, string name, [NotNullWhen(true)] out T? value);
    }
}
