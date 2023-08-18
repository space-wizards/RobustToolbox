using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    private readonly Dictionary<string, ToolshedCommandImplementor> _implementors = new();
    private readonly Dictionary<(CommandDiscriminator, string?), List<MethodInfo>> _concreteImplementations = new();

    public bool TryGetReturnType(string? subCommand, Type? pipedType, Type[] typeArguments,
        [NotNullWhen(true)] out Type? type)
    {
        var impls = GetConcreteImplementations(pipedType, typeArguments, subCommand).ToList();

        if (impls.Count > 0)
        {
            type = impls.First().ReturnType;
            return true;
        }

        type = null;
        return false;
    }

    internal List<MethodInfo> GetConcreteImplementations(Type? pipedType, Type[] typeArguments,
        string? subCommand)
    {
        var idx = (new CommandDiscriminator(pipedType, typeArguments), subCommand);
        if (_concreteImplementations.TryGetValue(idx,
                out var impl))
        {
            return impl;
        }

        impl = GetConcreteImplementationsInternal(pipedType, typeArguments, subCommand);

        _concreteImplementations[idx] = impl;
        return impl;
    }

    private List<MethodInfo> GetConcreteImplementationsInternal(Type? pipedType, Type[] typeArguments,
        string? subCommand)
    {
        var impls = GetGenericImplementations()
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .Where(x =>
            {
                if (x.ConsoleGetPipedArgument() is { } param)
                {
                    return pipedType?.IsAssignableToGeneric(param.ParameterType, Toolshed) ?? false;
                }

                return pipedType is null;
            })
            .OrderByDescending(x =>
            {
                if (x.ConsoleGetPipedArgument() is { } param)
                {
                    if (pipedType!.IsAssignableTo(param.ParameterType))
                        return 1000; // We want exact match to be preferred!

                    if (param.ParameterType.GetMostGenericPossible() == pipedType!.GetMostGenericPossible())
                        return 500; // If not, try to prefer the same base type.

                    // Finally, prefer specialized (type exact) implementations.
                    return param.ParameterType.IsGenericTypeParameter ? 0 : 100;
                }

                return 0;
            })
            .Where(x =>
            {
                if (x.IsGenericMethodDefinition)
                {
                    var expectedLen = x.GetGenericArguments().Length;
                    if (x.HasCustomAttribute<TakesPipedTypeAsGenericAttribute>())
                        expectedLen -= 1;

                    return typeArguments.Length == expectedLen;
                }

                return typeArguments.Length == 0;
            })
            .Select(x =>
            {
                try
                {
                    if (x.IsGenericMethodDefinition)
                    {
                        if (x.HasCustomAttribute<TakesPipedTypeAsGenericAttribute>())
                        {
                            var paramT = x.ConsoleGetPipedArgument()!.ParameterType;
                            var t = pipedType!.Intersect(paramT);
                            return x.MakeGenericMethod(typeArguments.Append(t).ToArray());
                        }
                        else
                            return x.MakeGenericMethod(typeArguments);
                    }

                    return x;
                }
                catch (ArgumentException)
                {
                    // oopsy, toolshed guessed wrong somewhere. Likely due to lack of constraint solver.
                    return null;
                }

            });

        return impls.Where(x => x is not null).Cast<MethodInfo>().ToList();
    }

    internal List<MethodInfo> GetGenericImplementations()
    {
        var t = GetType();

        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                                   BindingFlags.Instance);

        return methods.Where(x => x.HasCustomAttribute<CommandImplementationAttribute>()).ToList();
    }

    internal bool TryGetImplementation(Type? pipedType, string? subCommand, Type[] typeArguments,
        [NotNullWhen(true)] out Func<CommandInvocationArguments, object?>? impl)
    {
        return _implementors[subCommand ?? ""].TryGetImplementation(pipedType, typeArguments, out impl);
    }
}
