using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    public const BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                                            BindingFlags.Instance;

    public bool TryGetReturnType(
        string? subCommand,
        Type? pipedType,
        Type[]? typeArguments,
        [NotNullWhen(true)] out Type? type)
    {
        type = null;

        if (!CommandImplementors.TryGetValue(subCommand ?? string.Empty, out var impl))
            return false;

        if (!impl.TryGetConcreteMethod(pipedType, typeArguments, out var method))
            return false;

        type = method.Value.Info.ReturnType;
        return true;
    }

    internal IEnumerable<MethodInfo> GetGenericImplementations()
    {
        return GetType()
            .GetMethods(MethodFlags)
            .Where(x => x.HasCustomAttribute<CommandImplementationAttribute>());
    }
}
