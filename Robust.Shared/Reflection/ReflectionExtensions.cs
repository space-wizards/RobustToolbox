using System;
using System.Linq;
using System.Reflection;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Reflection
{
    internal static class ReflectionExtensions
    {
        internal static Type GetUnderlyingType(this MemberInfo member)
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

        internal static object? GetValue(this MemberInfo member, object instance)
        {
            return member switch
            {
                FieldInfo field => field.GetValue(instance),
                PropertyInfo property => property.GetValue(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(member))
            };
        }

        internal static void SetValue(this MemberInfo member, object instance, object? value)
        {
            switch (member)
            {
                case FieldInfo field:
                {
                    field.SetValue(instance, value);
                    return;
                }
                case PropertyInfo property:
                {
                    property.SetValue(instance, value);
                    return;
                }
            }
        }

        internal static MemberInfo? GetSingleMember(this Type type, string member, Type? declaringType = null)
        {
            var members = type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == member)
                .ToArray();

            if (members.Length == 0)
                return null;

            if (declaringType != null)
                return members.SingleOrDefault(m => m.DeclaringType == declaringType);

            return members.Length > 1
                // In case there's member hiding going on, grab the one declared by the type of the object by default.
                ? members.SingleOrDefault(m => m.DeclaringType == type)
                : members[0];
        }

        internal static PropertyInfo? GetIndexer(this Type type)
        {
            foreach (var pInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (pInfo.GetIndexParameters().Length == 0)
                    continue;

                return pInfo;
            }

            return null;
        }
    }
}
