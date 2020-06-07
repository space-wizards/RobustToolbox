using System;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public static class EnumHelper
    {
        public static bool TryParse(Type enumType, string value, bool ignoreCase, out object? enumValue)
        {
            return Enum.TryParse(enumType, value, ignoreCase, out enumValue);
        }
    }
}
