using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;

namespace Robust.Shared.Toolshed;

// This is for information about commands that can be queried, i.e. return type possibilities.

public sealed partial class ToolshedManager
{
    private readonly Dictionary<Type, List<CommandSpec>> _commandPipeValueMap = new();
    private readonly Dictionary<CommandSpec, HashSet<Type>> _commandReturnValueMap = new();

    private void InitializeQueries()
    {
        foreach (var (_, cmd) in _commands)
        {
            foreach (var (subcommand, methods) in cmd.GetGenericImplementations().BySubCommand())
            {
                foreach (var method in methods)
                {
                    var piped = method.ConsoleGetPipedArgument()?.ParameterType;

                    if (piped is null)
                        piped = typeof(void);

                    var list = GetTypeImplList(piped);
                    var invList = GetCommandRetValuesInternal(new CommandSpec(cmd, subcommand));
                    list.Add(new CommandSpec(cmd, subcommand == "" ? null : subcommand));
                    if (cmd.TryGetReturnType(subcommand, piped, Array.Empty<Type>(), out var retType) || method.ReturnType.Constructable())
                    {
                        invList.Add((retType ?? method.ReturnType));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Returns all commands that fit the given type constraints.
    /// </summary>
    /// <returns>Enumerable of matching command specs.</returns>
    public IEnumerable<CommandSpec> CommandsFittingConstraint(Type input, Type output)
    {
        foreach (var (command, subcommand) in CommandsTakingType(input))
        {
            if (command.HasTypeParameters)
                continue; // We don't consider these right now.

            var impls = command.GetConcreteImplementations(input, Array.Empty<Type>(), subcommand);

            foreach (var impl in impls)
            {
                if (impl.ReturnType.IsAssignableTo(output))
                    yield return new CommandSpec(command, subcommand);
            }
        }
    }

    /// <summary>
    ///     Returns all commands that accept the given type.
    /// </summary>
    /// <param name="t">Type to use in the query.</param>
    /// <returns>Enumerable of matching command specs.</returns>
    /// <remarks>Not currently type constraint aware.</remarks>
    public IEnumerable<CommandSpec> CommandsTakingType(Type t)
    {
        var output = new Dictionary<(string, string?), CommandSpec>();
        foreach (var type in AllSteppedTypes(t))
        {
            var list = GetTypeImplList(type);
            if (type.IsGenericType)
            {
                list = list.Concat(GetTypeImplList(type.GetGenericTypeDefinition())).ToList();
            }
            foreach (var entry in list)
            {
                output.TryAdd((entry.Cmd.Name, entry.SubCommand), entry);
            }
        }

        return output.Values;
    }

    private Dictionary<Type, HashSet<Type>> _typeCache = new();

    internal IEnumerable<Type> AllSteppedTypes(Type t, bool allowVariants = true)
    {
        if (_typeCache.TryGetValue(t, out var cache))
            return cache;
        cache = new(AllSteppedTypesInner(t, allowVariants));
        _typeCache[t] = cache;

        return cache;
    }

    private IEnumerable<Type> AllSteppedTypesInner(Type t, bool allowVariants)
    {
        Type oldT;
        do
        {
            yield return t;
            if (t == typeof(void))
                yield break;

            if (t.IsGenericType && allowVariants)
            {
                foreach (var variant in t.GetVariants(this))
                {
                    yield return variant;
                }
            }

            foreach (var @interface in t.GetInterfaces())
            {
                foreach (var innerT in AllSteppedTypes(@interface, allowVariants))
                {
                    yield return innerT;
                }
            }

            if (t.BaseType is { } baseType)
            {
                foreach (var innerT in AllSteppedTypes(baseType, allowVariants))
                {
                    yield return innerT;
                }
            }

            yield return typeof(IEnumerable<>).MakeGenericType(t);

            oldT = t;
            t = t.StepDownConstraints();
        } while (t != oldT);
    }

    /// <summary>
    ///     Attempts to return the return values of the given command, if they can be decided.
    /// </summary>
    /// <remarks>
    ///     Generics are flat out uncomputable so this doesn't bother.
    /// </remarks>
    public IReadOnlySet<Type> GetCommandRetValues(CommandSpec command)
        => GetCommandRetValuesInternal(command);

    private HashSet<Type> GetCommandRetValuesInternal(CommandSpec command)
    {
        if (!_commandReturnValueMap.TryGetValue(command, out var l))
        {
            l = new();
            _commandReturnValueMap[command] = l;
        }

        return l;
    }

    private List<CommandSpec> GetTypeImplList(Type t)
    {
        if (!t.Constructable())
        {
            if (t.IsGenericParameter)
            {
                var constraints = t.GetGenericParameterConstraints();

                // for now be dumb.
                if (constraints.Length > 0 && !constraints.First().IsConstructedGenericType)
                    return GetTypeImplList(constraints.First());
                return GetTypeImplList(typeof(object));
            }

            t = t.GetGenericTypeDefinition();
        }

        if (t.IsGenericType && !t.IsConstructedGenericType)
        {
            t = t.GetGenericTypeDefinition();
        }

        if (!_commandPipeValueMap.TryGetValue(t, out var l))
        {
            l = new();
            _commandPipeValueMap[t] = l;
        }

        return l;
    }
}
