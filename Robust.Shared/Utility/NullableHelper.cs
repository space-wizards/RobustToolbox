using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.CompilerServices.SymbolWriter;

namespace Robust.Shared.Utility
{
    public static class NullableHelper
    {
        private delegate byte[] GetNullableFlagDelegate(Attribute t);
        private static GetNullableFlagDelegate? _getNullableFlag;
        private static Type? _nullableAttributeType;

        public static bool IsMarkedAsNullable(FieldInfo field)
        {
            if (Nullable.GetUnderlyingType(field.FieldType) != null) return true;

            var flags = GetNullableFlags(field);
            if (flags.Length == 0) return false;
            return flags[0] == 2;
        }

        public static byte[] GetNullableFlags(FieldInfo field)
        {
            //TODO is this fine? D:
            _nullableAttributeType ??= Assembly.GetCallingAssembly().GetType("System.Runtime.CompilerServices.NullableAttribute");

            if (_getNullableFlag == null)
            {
                CacheGetNullableFlag();
            }

            if (_nullableAttributeType == null)
            {
                throw new Exception("NullableAttribute Type not found");
            }

            var nullableAttribute = field.GetCustomAttribute(_nullableAttributeType);
            if (nullableAttribute == null)
            {
                return new byte[]{};
            }

            return _getNullableFlag!(nullableAttribute);
        }

        private static void CacheGetNullableFlag()
        {
            var dynamicMethod = new DynamicMethod($"_getter<>nullableAttributeByte", typeof(byte[]), new Type[]{typeof(object)}, typeof(NullableHelper), true);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "attribute");

            var generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            var field = _nullableAttributeType?.GetField("NullableFlags");
            if (field == null)
            {
                throw new Exception("NullableFlags field not found");
            }
            generator.Emit(OpCodes.Ldfld, field);
            generator.Emit(OpCodes.Ret);

            _getNullableFlag = (GetNullableFlagDelegate)dynamicMethod.CreateDelegate(typeof(GetNullableFlagDelegate));
        }
    }
}
