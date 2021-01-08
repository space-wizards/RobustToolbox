using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IDeepClone
    {
        IDeepClone DeepClone();

        public static T? CloneValue<T>(T? value)
        {
            if (value == null) return default;
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal) || underlyingType == typeof(String) || type.IsEnum)
                return value;

            if (typeof(IDeepClone).IsAssignableFrom(type))
                return (T)((IDeepClone) value).DeepClone();

            if (type.IsArray && value is Array arraySource)
            {
                var newArray = (Array)Activator.CreateInstance(type, arraySource.Length)!;

                var idx = 0;
                foreach (var entry in arraySource)
                {
                    newArray.SetValue(CloneValue(entry), idx++);
                }

                return newArray is T array ? array : default;
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

                return (T)newList;
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

                return (T)newList;
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

                return (T)newDict;
            }

            // HashSet<T>
            if (TypeHelpers.TryGenericHashSetType(type, out var setType) && value is Array setSource)
            {
                var valuesArray = Array.CreateInstance(setType, new[] {setSource.Length})!;

                for (var i = 0; i < setSource.Length; i++)
                {
                    valuesArray.SetValue(CloneValue(setSource.GetValue(i)), i);
                }

                var newSet = Activator.CreateInstance(type, valuesArray)!;

                return (T)newSet;
            }

            throw new ArgumentException($"Failed to clone value with type {type}", nameof(value));
        }

    }
}
