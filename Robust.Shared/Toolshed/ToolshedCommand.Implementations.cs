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

    internal MethodInfo[] GetMethods()
    {
        var methods = GetType().GetMethods(MethodFlags);

        // CommandImplementationAttribute is optional if there is only a single method defined by the type,
        return methods.Length == 1
            ? methods
            : methods.Where(x => x.HasCustomAttribute<CommandImplementationAttribute>()).ToArray();
    }

    internal MethodInfo[] GetMethods(string? subCommand)
    {
        if (subCommand == null)
            return GetMethods();

        return GetType()
            .GetMethods(MethodFlags)
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .ToArray();
    }
}
