using System;
using System.Reflection;

namespace Robust.Shared.Reflection
{
    internal static class ReflectionExtensions
    {
        public static Type GetUnderlyingType(this MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Event => ((EventInfo) member).EventHandlerType!,
                MemberTypes.Field => ((FieldInfo) member).FieldType,
                MemberTypes.Method => ((MethodInfo) member).ReturnType,
                MemberTypes.Property => ((PropertyInfo) member).PropertyType,
                _ => throw new ArgumentException("MemberInfo must be one of: EventInfo, FieldInfo, MethodInfo, PropertyInfo")
            };
        }
    }
}
