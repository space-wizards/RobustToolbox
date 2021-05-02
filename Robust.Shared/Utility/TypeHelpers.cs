using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
                foreach (var field in p.GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public))
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
                p.GetProperties(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly |
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
                foreach (var field in p.GetNestedTypes(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public))
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

        public static Type? SelectCommonType(Type type1, Type type2)
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

        public static SpecificFieldInfo? GetBackingField(this Type type, string propertyName)
        {
            foreach (var parent in type.GetClassHierarchy())
            {
                var field = parent.GetField($"<{propertyName}>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    return new SpecificFieldInfo(field);
                }
            }

            return null;
        }

        public static bool HasBackingField(this Type type, string propertyName)
        {
            return type.GetBackingField(propertyName) != null;
        }

        public static bool TryGetBackingField(this Type type, string propertyName,
            [NotNullWhen(true)] out SpecificFieldInfo? field)
        {
            return (field = type.GetBackingField(propertyName)) != null;
        }

        public static bool IsBackingField(this MemberInfo memberInfo)
        {
            return memberInfo.HasCustomAttribute<CompilerGeneratedAttribute>() &&
                   memberInfo.Name.StartsWith("<") &&
                   memberInfo.Name.EndsWith(">k__BackingField");
        }

        public static bool HasCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo.GetCustomAttribute<T>() != null;
        }

        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, [NotNullWhen(true)] out T? attribute)
            where T : Attribute
        {
            return (attribute = memberInfo.GetCustomAttribute<T>()) != null;
        }
    }
}
