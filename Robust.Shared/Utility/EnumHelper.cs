using System;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public static class EnumHelper
    {
        //TODO: This function is obsloete in .Net Core 3 https://github.com/dotnet/corefx/issues/692
        //Credit: https://justinmchase.com/2010/07/09/non-generic-enumtryparse/

        static MethodInfo enumTryParse;

        static EnumHelper()
        {
            enumTryParse = typeof(Enum)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 3);
        }

        public static bool TryParse(Type enumType, string value, bool ignoreCase, out object enumValue)
        {
            var genericEnumTryParse = enumTryParse.MakeGenericMethod(enumType);

            object[] args = { value, ignoreCase, Enum.ToObject(enumType, 0) };
            var success = (bool)genericEnumTryParse.Invoke(null, args);
            enumValue = args[2];

            return success;
        }
    }
}
