using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public static class NullableHelper
    {
        private const int NotAnnotatedNullableFlag = 1;

        private static readonly Dictionary<Assembly, (Type AttributeType, FieldInfo NullableFlagsField)?>
            _nullableAttributeTypeCache = new();

        private static readonly Dictionary<Assembly, (Type AttributeType, FieldInfo FlagsField)?>
            _nullableContextAttributeTypeCache = new();

        public static Type EnsureNullableType(this Type type)
        {
            if (type.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        public static Type EnsureNotNullableType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Checks if the field has a nullable annotation [?]
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsMarkedAsNullable(FieldInfo field)
        {
            if (Nullable.GetUnderlyingType(field.FieldType) != null) return true;

            var flags = GetNullableFlags(field);
            if (flags.Length != 0 && flags[0] != NotAnnotatedNullableFlag) return true;

            if (field.DeclaringType == null) return false;

            var cflag = GetNullableContextFlag(field.DeclaringType);
            return cflag != NotAnnotatedNullableFlag;
        }

        public static bool IsNullable(this Type type)
        {
            return IsNullable(type, out _);
        }

        public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType == null)
            {
                return false;
            }

            return true;
        }

        private static byte[] GetNullableFlags(FieldInfo field)
        {
            lock (_nullableAttributeTypeCache)
            {
                Assembly assembly = field.Module.Assembly;
                if (!_nullableAttributeTypeCache.TryGetValue(assembly, out var assemblyNullableEntry))
                {
                    CacheNullableFieldInfo(assembly);
                }
                assemblyNullableEntry = _nullableAttributeTypeCache[assembly];

                if (assemblyNullableEntry == null)
                {
                    return new byte[]{0};
                }

                var nullableAttribute = field.GetCustomAttribute(assemblyNullableEntry.Value.AttributeType);
                if (nullableAttribute == null)
                {
                    return new byte[]{1};
                }

                return assemblyNullableEntry.Value.NullableFlagsField.GetValue(nullableAttribute) as byte[] ?? new byte[]{1};
            }
        }

        private static byte GetNullableContextFlag(Type type)
        {
            lock (_nullableContextAttributeTypeCache)
            {
                Assembly assembly = type.Assembly;
                if (!_nullableContextAttributeTypeCache.TryGetValue(assembly, out var assemblyNullableEntry))
                {
                    CacheNullableContextFieldInfo(assembly);
                }
                assemblyNullableEntry = _nullableContextAttributeTypeCache[assembly];

                if (assemblyNullableEntry == null)
                {
                    return 0;
                }

                var nullableAttribute = type.GetCustomAttribute(assemblyNullableEntry.Value.AttributeType);
                if (nullableAttribute == null)
                {
                    return 1;
                }

                return (byte) (assemblyNullableEntry.Value.FlagsField.GetValue(nullableAttribute) ?? 1);
            }
        }

        private static void CacheNullableFieldInfo(Assembly assembly)
        {
            var nullableAttributeType = assembly.GetType("System.Runtime.CompilerServices.NullableAttribute");
            if (nullableAttributeType == null)
            {
                _nullableAttributeTypeCache.Add(assembly, null);
                return;
            }

            var field = nullableAttributeType.GetField("NullableFlags");
            if (field == null)
            {
                _nullableAttributeTypeCache.Add(assembly, null);
                return;
            }

            _nullableAttributeTypeCache.Add(assembly, (nullableAttributeType, field));
        }

        private static void CacheNullableContextFieldInfo(Assembly assembly)
        {
            var nullableContextAttributeType =
                assembly.GetType("System.Runtime.CompilerServices.NullableContextAttribute");
            if (nullableContextAttributeType == null)
            {
                _nullableContextAttributeTypeCache.Add(assembly, null);
                return;
            }

            var field = nullableContextAttributeType.GetField("Flag");
            if (field == null)
            {
                _nullableContextAttributeTypeCache.Add(assembly, null);
                return;
            }

            _nullableContextAttributeTypeCache.Add(assembly, (nullableContextAttributeType, field));
        }
    }
}
