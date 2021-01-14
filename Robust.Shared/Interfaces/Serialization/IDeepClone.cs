using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IDeepClone
    {
        IDeepClone DeepClone();

        private static Dictionary<Type, Type>? _deepCloneExtensions;

        private static void CacheDeepCloneExtensions()
        {
            _deepCloneExtensions = new();
            foreach(var extension in IoCManager.Resolve<IReflectionManager>().FindTypesWithAttribute<DeepCloneExtensionAttribute>())
            {
                if(Attribute.GetCustomAttribute(extension, typeof(DeepCloneExtensionAttribute)) is DeepCloneExtensionAttribute attr)
                    _deepCloneExtensions.Add(attr.ForType, extension);
            }
        }

        public static T? CloneValue<T>(T? value)
        {
            if (value == null) return default;
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal) || underlyingType == typeof(String) || type.IsEnum)
                return value;

            if (typeof(IDeepClone).IsAssignableFrom(underlyingType))
                return (T)((IDeepClone) value).DeepClone();

            if (underlyingType.IsArray && value is Array arraySource)
            {
                var newArray = (Array)Activator.CreateInstance(underlyingType, arraySource.Length)!;

                var idx = 0;
                foreach (var entry in arraySource)
                {
                    newArray.SetValue(CloneValue(entry), idx++);
                }

                return newArray is T array ? array : default;
            }

            // IReadOnlyList<T>/IReadOnlyCollection<T>
            if (TypeHelpers.TryGenericReadOnlyCollectionType(underlyingType, out var collectionType))
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
            if (TypeHelpers.TryGenericListType(underlyingType, out var listType))
            {
                var source = (IList)value;
                var newList = (IList)Activator.CreateInstance(underlyingType, source.Count)!;

                foreach (var entry in source)
                {
                    newList.Add(CloneValue(entry));
                }

                return (T)newList;
            }

            // Dictionary<K,V>/IReadOnlyDictionary<K,V>
            if (TypeHelpers.TryGenericReadDictType(underlyingType, out var keyType, out var valType, out var dictType))
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

            if (TypeHelpers.TryGenericSortedDictType(underlyingType, out var sortedKeyType, out var sortedValType))
            {
                var sourceDict = (IDictionary) value;
                //var valueDict = (IDictionary) Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(sortedKeyType, sortedValType))!;
                var newDict = (IDictionary)Activator.CreateInstance(underlyingType)!;
                foreach (DictionaryEntry entry in sourceDict)
                {
                    newDict.Add(CloneValue(entry.Key)!, CloneValue(entry.Value));
                }

                return (T)newDict;
            }

            // HashSet<T>
            if ((TypeHelpers.TryGenericHashSetType(underlyingType, out var setType) || TypeHelpers.TryGenericSortedSetType(underlyingType, out setType)) && value is IEnumerable rawSetSource)
            {
                List<object?> values = new();
                foreach (var val in rawSetSource)
                {
                    values.Add(CloneValue(val));
                }

                var newSet = Activator.CreateInstance(underlyingType, values.ToArray())!;

                return (T)newSet;
            }

            if(_deepCloneExtensions == null) CacheDeepCloneExtensions();
            if (_deepCloneExtensions != null && _deepCloneExtensions.TryGetValue(underlyingType, out var extension))
            {
                var extensionInstance = (DeepCloneExtension?) Activator.CreateInstance(extension);
                if (extensionInstance != null)
                {
                    return (T)extensionInstance.DeepClone(value);
                }
                else{
                    Logger.Error($"Failed to create deepcloneextension {extension} for type {type}");
                }
            }

            //class fallback
            Exception? e = null;
            try
            {
                Logger.Warning($"Using Fallback-Deepclone for Type {type}!");
                var fields = underlyingType.GetAllFields().ToArray();
                if (fields.Length != 0)
                {
                    var newInstance = Activator.CreateInstance(underlyingType);
                    foreach (var fieldInfo in fields)
                    {
                        var tempVal = fieldInfo.GetValue(value);
                        fieldInfo.SetValue(newInstance, CloneValue(tempVal));
                    }

                    return newInstance is T ? (T) newInstance : default;
                }
            }
            catch (Exception err)
            {
                // ignored
                e = err;
            }

            Logger.Error($"Failed to clone value with type {type}{(e != null ? $" with error {e}" : "")}", nameof(value));
            return value;
        }

    }
}
