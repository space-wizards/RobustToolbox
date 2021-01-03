using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IDeepClone
    {
        IDeepClone DeepClone();

        public static object? CloneValue(object? value)
        {
            if (value == null) return null;
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
                return value;

            if (typeof(IDeepClone).IsAssignableFrom(type))
                return ((IDeepClone) value).DeepClone();

            if (type.IsArray)
            {
                var source = (Array)value;
                var newArray = (Array)Activator.CreateInstance(type, source.Length)!;

                var idx = 0;
                foreach (var entry in source)
                {
                    newArray.SetValue(CloneValue(entry), idx++);
                }

                return newArray;
            }

            // IReadOnlyList<T>/IReadOnlyCollection<T>
            if (TypeHelpers.TryGenericReadOnlyCollectionType(type, out var collectionType))
            {
                var source = (IList)value;
                var newList = (IList)Array.CreateInstance(collectionType, source.Count);

                for (var i = 0; i < source.Count; i++)
                {
                    newList[i] = CloneValue(source[i]);
                }

                return newList;
            }

            // List<T>
            if (TypeHelpers.TryGenericListType(type, out var listType))
            {
                var source = (IList)value;
                var newList = (IList)Activator.CreateInstance(type, source.Count)!;

                foreach (var entry in source)
                {
                    newList.Add(CloneValue(entry));
                }

                return newList;
            }

            // Dictionary<K,V>/IReadOnlyDictionary<K,V>
            if (TypeHelpers.TryGenericReadDictType(type, out var keyType, out var valType, out var dictType))
            {
                var sourceDict = (IDictionary)value;
                var newDict = (IDictionary)Activator.CreateInstance(dictType, sourceDict.Count)!;

                foreach (DictionaryEntry entry in sourceDict)
                {
                    //we can ignore the nullabilityerror here since we only receive null if we pass in null
                    //sources: dude trust me
                    newDict.Add(CloneValue(entry.Key)!, CloneValue(entry.Value));
                }

                return newDict;
            }

            // HashSet<T>
            if (TypeHelpers.TryGenericHashSetType(type, out var setType))
            {
                var source = (Array) value;
                var valuesArray = Array.CreateInstance(setType, new[] {source.Length})!;

                for (var i = 0; i < source.Length; i++)
                {
                    valuesArray.SetValue(CloneValue(source.GetValue(i)), i);
                }

                var newSet = Activator.CreateInstance(type, valuesArray)!;

                return newSet;
            }

            throw new ArgumentException($"Failed to clone value with type {type}", nameof(value));
        }

    }
}
