using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public static class NullableHelper
    {
        //
        // Since .NET 8, System.Runtime.CompilerServices.NullableAttribute is included in the BCL.
        // Before this, Roslyn emitted a copy of the attribute into every assembly compiled.
        // In the latter case we need to find the type for every assembly that has it.
        // Yeah most of this code can probably be removed now but just for safety I'm keeping it as a fallback path.
        //

        private const int NotAnnotatedNullableFlag = 1;

        private static readonly Type? BclNullableCache;
        private static readonly Type? BclNullableContextCache;

        static NullableHelper()
        {
            BclNullableCache = Type.GetType("System.Runtime.CompilerServices.NullableAttribute");
            BclNullableContextCache = Type.GetType("System.Runtime.CompilerServices.NullableContextAttribute");
        }

        private static readonly Dictionary<Assembly, (Type AttributeType, FieldInfo NullableFlagsField)?>
            _nullableAttributeTypeCache = new();

        private static readonly Dictionary<Assembly, (Type AttributeType, FieldInfo FlagsField)?>
            _nullableContextAttributeTypeCache = new();

        //todo paul remove this shitty hack once serv3 nullable reference is sane again
        public static Type? GetUnderlyingType(this Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null) return underlyingType;

            return type.IsValueType ? null : type;
        }

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
            return GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Checks if the field has a nullable annotation [?]
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        internal static bool IsMarkedAsNullable(AbstractFieldInfo field)
        {
            //to understand whats going on here, read https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md
            if (Nullable.GetUnderlyingType(field.FieldType) != null) return true;

            var flags = GetNullableFlags(field);
            if (flags.Length != 0 && flags[0] != NotAnnotatedNullableFlag) return true;

            if (field.DeclaringType == null || field.FieldType.IsValueType) return false;

            var cflag = GetNullableContextFlag(field.DeclaringType);
            return cflag != NotAnnotatedNullableFlag;
        }

        public static bool IsNullable(this Type type)
        {
            return IsNullable(type, out _);
        }

        public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? underlyingType)
        {
            underlyingType = GetUnderlyingType(type);

            if (underlyingType == null)
            {
                return false;
            }

            return true;
        }

        private static byte[] GetNullableFlags(AbstractFieldInfo field)
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

                if (!field.TryGetAttribute(assemblyNullableEntry.Value.AttributeType, out var nullableAttribute))
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
            nullableAttributeType ??= BclNullableCache;
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
            nullableContextAttributeType ??= BclNullableContextCache;
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
