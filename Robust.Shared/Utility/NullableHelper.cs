using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.CompilerServices.SymbolWriter;
using Robust.Shared.Log;

namespace Robust.Shared.Utility
{
    public static class NullableHelper
    {
        private const int AnnotatedNullableFlag = 2;

        private static Dictionary<Assembly, (Type AttributeType, FieldInfo NullableFlagsField)> _nullableAttributeTypeCache = new Dictionary<Assembly, (Type AttributeType, FieldInfo NullableFlagsField)>();

        private static Dictionary<Assembly, (Type AttributeType, FieldInfo FlagsField)> _nullableContextAttributeTypeCache = new Dictionary<Assembly, (Type AttributeType, FieldInfo FlagsField)>();

        /// <summary>
        /// Checks if the field has a nullable annotation [?]
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsMarkedAsNullable(FieldInfo field)
        {
            if (Nullable.GetUnderlyingType(field.FieldType) != null) return true;

            var flags = GetNullableFlags(field);
            if (flags.Length != 0 && flags[0] == AnnotatedNullableFlag) return true;

            if (field.DeclaringType == null) return false;

            var cflag = GetNullableContextFlag(field.DeclaringType);
            return cflag == AnnotatedNullableFlag;
        }

        private static byte[] GetNullableFlags(FieldInfo field)
        {
            Assembly assembly = field.Module.Assembly;
            if (!_nullableAttributeTypeCache.TryGetValue(assembly, out var assemblyNullableEntry))
            {
                try
                {
                    CacheNullableFieldInfo(assembly);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error while trying to get NullableFieldInfo for Assembly {assembly}. Error: {e.Message}");
                    return new byte[0];
                }
            }
            assemblyNullableEntry = _nullableAttributeTypeCache[assembly];

            var nullableAttribute = field.GetCustomAttribute(assemblyNullableEntry.AttributeType);
            if (nullableAttribute == null)
            {
                return new byte[0];
            }

            return assemblyNullableEntry.NullableFlagsField.GetValue(nullableAttribute) as byte[] ?? new byte[0];
        }

        private static byte GetNullableContextFlag(Type type)
        {
            Assembly assembly = type.Assembly;
            if (!_nullableContextAttributeTypeCache.TryGetValue(assembly, out var assemblyNullableEntry))
            {
                try
                {
                    CacheNullableContextFieldInfo(assembly);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error while trying to get NullableContextFieldInfo for Assembly {assembly}. Error: {e.Message}");
                    return 0;
                }
            }
            assemblyNullableEntry = _nullableContextAttributeTypeCache[assembly];

            var nullableAttribute = type.GetCustomAttribute(assemblyNullableEntry.AttributeType);
            if (nullableAttribute == null)
            {
                return 0;
            }

            return (byte) (assemblyNullableEntry.FlagsField.GetValue(nullableAttribute) ?? 0);
        }

        private static void CacheNullableFieldInfo(Assembly assembly)
        {
            var nullableAttributeType = assembly.GetType("System.Runtime.CompilerServices.NullableAttribute");
            if (nullableAttributeType == null)
            {
                throw new Exception($"No System.Runtime.CompilerServices.NullableAttribute found in Assembly {assembly}");
            }

            var field = nullableAttributeType.GetField("NullableFlags");
            if (field == null)
            {
                throw new Exception("NullableFlags field not found");
            }
            _nullableAttributeTypeCache.Add(assembly, (nullableAttributeType, field));
        }

        private static void CacheNullableContextFieldInfo(Assembly assembly)
        {
            var nullableContextAttributeType = assembly.GetType("System.Runtime.CompilerServices.NullableContextAttribute");
            if (nullableContextAttributeType == null)
            {
                throw new Exception($"No System.Runtime.CompilerServices.NullableContext found in Assembly {assembly}");
            }

            var field = nullableContextAttributeType.GetField("Flag");
            if (field == null)
            {
                throw new Exception("Flag field not found");
            }
            _nullableContextAttributeTypeCache.Add(assembly, (nullableContextAttributeType, field));
        }
    }
}
