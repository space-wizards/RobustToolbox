using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public static class TypeHelpers
    {
        /// <summary>
        ///     Returns absolutely all fields, privates, readonlies, and ones from parents.
        /// </summary>
        public static IEnumerable<FieldInfo> GetAllFields(this Type t)
        {
            // We need to fetch the entire class hierarchy and SelectMany(),
            // Because BindingFlags.FlattenHierarchy doesn't read privates,
            // Even when you pass BindingFlags.NonPublic.
            foreach (var p in GetClassHierarchy(t))
            {
                foreach (var field in p.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |
                                                  BindingFlags.DeclaredOnly | BindingFlags.Public))
                {
                    yield return field;
                }
            }
        }

        /// <summary>
        ///     Returns absolutely all instance properties on a type. Inherited and private included.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type t)
        {
            return GetClassHierarchy(t).SelectMany(p =>
                p.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly |
                                BindingFlags.Public));
        }

        /// <summary>
        ///     Checks whether a property is the "base" definition.
        ///     So basically it returns false for overrides.
        /// </summary>
        public static bool IsBasePropertyDefinition(this PropertyInfo propertyInfo)
        {
            foreach (var accessor in propertyInfo.GetAccessors())
            {
                if (!accessor.IsVirtual)
                {
                    continue;
                }

                if (accessor.GetBaseDefinition() != accessor)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<Type> GetClassHierarchy(this Type t)
        {
            yield return t;

            while (t.BaseType != null)
            {
                t = t.BaseType;
                yield return t;
            }
        }

        /// <summary>
        ///     Returns ALL nested types of the specified type, including private types of its parent.
        /// </summary>
        public static IEnumerable<Type> GetAllNestedTypes(this Type t)
        {
            foreach (var p in GetClassHierarchy(t))
            {
                foreach (var field in p.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly | BindingFlags.Public))
                {
                    yield return field;
                }
            }
        }

        internal static readonly IComparer<Type> TypeInheritanceComparer = new TypeInheritanceComparerImpl();

        public sealed class TypeInheritanceComparerImpl : IComparer<Type>
        {
            public int Compare(Type? x, Type? y)
            {
                if (x == null || y == null || x == y)
                {
                    return 0;
                }

                if (x.IsAssignableFrom(y))
                {
                    return -1;
                }

                if (y.IsAssignableFrom(x))
                {
                    return 1;
                }

                return 0;
            }
        }

        public static bool TryGenericReadOnlyCollectionType(Type type, [NotNullWhen(true)] out Type? listType)
        {
            if (!type.GetTypeInfo().IsGenericType)
            {
                listType = default;
                return false;
            }

            var baseGeneric = type.GetGenericTypeDefinition();
            var isList = baseGeneric == typeof(IReadOnlyCollection<>) || baseGeneric == typeof(IReadOnlyList<>);

            if (isList)
            {
                listType = type.GetGenericArguments()[0];
                return true;
            }

            listType = default;
            return false;
        }

        public static bool TryGenericListType(Type type, [NotNullWhen(true)] out Type? listType)
        {
            var isList = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

            if (isList)
            {
                listType = type.GetGenericArguments()[0];
                return true;
            }

            listType = default;
            return false;
        }

        public static bool TryGenericReadOnlyDictType(Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        public static bool TryGenericReadDictType(Type type, [NotNullWhen(true)] out Type? keyType,
            [NotNullWhen(true)] out Type? valType, [NotNullWhen(true)] out Type? dictType)
        {
            if (TryGenericDictType(type, out keyType, out valType))
            {
                // Pass through the type directly if it's Dictionary<K,V>.
                // Since that's more efficient.
                dictType = type;
                return true;
            }

            if (TryGenericReadOnlyDictType(type, out keyType, out valType))
            {
                // If it's IReadOnlyDictionary<K,V> we need to make a Dictionary<K,V> type to use to deserialize.
                dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
                return true;
            }

            dictType = default;
            return false;
        }

        public static bool TryGenericDictType(Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        public static bool TryGenericSortedDictType(Type type, [NotNullWhen(true)] out Type? keyType,
            [NotNullWhen(true)] out Type? valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedDictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        public static bool TryGenericHashSetType(Type type, [NotNullWhen(true)] out Type? setType)
        {
            var isSet = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);

            if (isSet)
            {
                setType = type.GetGenericArguments()[0];
                return true;
            }

            setType = default;
            return false;
        }

        public static bool TryGenericSortedSetType(Type type, [NotNullWhen(true)] out Type? setType)
        {
            var isSet = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedSet<>);

            if (isSet)
            {
                setType = type.GetGenericArguments()[0];
                return true;
            }

            setType = default;
            return false;
        }

        public static IEnumerable<AbstractFieldInfo> GetAllPropertiesAndFields(this Type type)
        {
            foreach (var field in type.GetAllFields())
            {
                yield return new SpecificFieldInfo(field);
            }

            foreach (var property in type.GetAllProperties())
            {
                yield return new SpecificPropertyInfo(property);
            }
        }

        public static Type? FindCommonType(Type type1, Type type2)
        {
            Type? commonType = null;
            if (type1.IsAssignableFrom(type2))
            {
                commonType = type1;
            }else if (type2.IsAssignableFrom(type1))
            {
                commonType = type2;
            }

            return commonType;
        }
    }

    public abstract class AbstractFieldInfo
    {
        public abstract Type FieldType { get; }
        public abstract Type? DeclaringType { get; }

        public abstract object? GetValue(object? obj);
        public abstract void SetValue(object? obj, object? value);

        public abstract T? GetCustomAttribute<T>() where T : Attribute;
    }

    public class SpecificFieldInfo : AbstractFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public override Type FieldType => FieldInfo.FieldType;
        public override Type? DeclaringType => FieldInfo.DeclaringType;

        public SpecificFieldInfo(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }

        public override object? GetValue(object? obj) => FieldInfo.GetValue(obj);
        public override void SetValue(object? obj, object? value) => FieldInfo.SetValue(obj, value);

        public override T? GetCustomAttribute<T>() where T : class
        {
            return (T?)Attribute.GetCustomAttribute(FieldInfo, typeof(T));
        }

        public static implicit operator FieldInfo(SpecificFieldInfo f) => f.FieldInfo;
        public static explicit operator SpecificFieldInfo(FieldInfo f) => new(f);
    }

    public class SpecificPropertyInfo : AbstractFieldInfo
    {
        public readonly PropertyInfo PropertyInfo;
        public override Type FieldType => PropertyInfo.PropertyType;
        public override Type? DeclaringType => PropertyInfo.DeclaringType;

        public SpecificPropertyInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public override object? GetValue(object? obj) => PropertyInfo.GetValue(obj);
        public override void SetValue(object? obj, object? value) => PropertyInfo.SetValue(obj, value);

        public override T? GetCustomAttribute<T>() where T : class
        {
            return (T?)Attribute.GetCustomAttribute(PropertyInfo, typeof(T));
        }

        public static implicit operator PropertyInfo(SpecificPropertyInfo f) => f.PropertyInfo;
        public static explicit operator SpecificPropertyInfo(PropertyInfo f) => new(f);
    }
}
