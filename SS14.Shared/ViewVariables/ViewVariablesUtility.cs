using System;
using System.Linq;
using System.Reflection;
using SS14.Shared.Utility;

namespace SS14.Shared.ViewVariables
{
    public static class ViewVariablesUtility
    {
        /// <summary>
        ///     Check whether this type has any fields or properties with the <see cref="ViewVariablesAttribute"/>,
        ///     i.e. whether there are any members that are visible in a VV window.
        /// </summary>
        /// <remarks>
        ///     This is quite an expensive operation, don't use it lightly.
        /// </remarks>
        /// <param name="type">The type to check.</param>
        /// <returns>True if there are members with the attribute, false otherwise.</returns>
        public static bool TypeHasVisibleMembers(Type type)
        {
            return type.GetAllFields().Cast<MemberInfo>().Concat(type.GetAllProperties())
                .Any(f => f.GetCustomAttribute<ViewVariablesAttribute>() != null);
        }
    }
}
