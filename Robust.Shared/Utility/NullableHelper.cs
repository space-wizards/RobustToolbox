using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.CompilerServices.SymbolWriter;

namespace Robust.Shared.Utility
{
    public static class NullableHelper
    {
        private delegate byte[] GetNullableFlagDelegate(Attribute t);
        private static Dictionary<Assembly, (Type, FieldInfo)> _nullableAttributeTypeCache = new Dictionary<Assembly, (Type, FieldInfo)>();

        /// <summary>
        /// Checks if the field has a nullable annotation [?]
        /// DOES NOT WORK WITH Object? AND object?
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsMarkedAsNullable(FieldInfo field)
        {
            if (Nullable.GetUnderlyingType(field.FieldType) != null) return true;

            var flags = GetNullableFlags(field);
            if (flags.Length == 0) return false;
            return flags[0] == 2;
        }

        private static byte[] GetNullableFlags(FieldInfo field)
        {
            Assembly assembly = field.Module.Assembly;
            if (!_nullableAttributeTypeCache.TryGetValue(assembly, out var assemblyNullableEntry))
            {
                CacheNullableFlagFieldInfo(assembly);
            }
            assemblyNullableEntry = _nullableAttributeTypeCache[assembly];

            var nullableAttribute = field.GetCustomAttribute(assemblyNullableEntry.Item1);
            if (nullableAttribute == null)
            {
                return new byte[0];
            }

            return assemblyNullableEntry.Item2.GetValue(nullableAttribute) as byte[] ?? new byte[0];
        }

        private static void CacheNullableFlagFieldInfo(Assembly assembly)
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
    }
}
