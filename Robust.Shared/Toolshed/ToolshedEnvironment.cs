using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Robust.Shared.Toolshed;

public sealed class ToolshedEnvironment
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ToolshedManager _toolshedManager = default!;
    private readonly Dictionary<string, ToolshedCommand> _commands = new();
    private readonly Dictionary<Type, List<CommandSpec>> _commandPipeValueMap = new();
    private readonly Dictionary<CommandSpec, HashSet<Type>> _commandReturnValueMap = new();


    private ISawmill _log = default!;

    /// <summary>
    ///     Provides every registered command, including subcommands.
    /// </summary>
    /// <returns>Enumerable of every command.</returns>
    public IEnumerable<CommandSpec> AllCommands()
    {
        foreach (var (_, cmd) in _commands)
        {
            if (cmd.HasSubCommands)
            {
                foreach (var subcommand in cmd.Subcommands)
                {
                    yield return new(cmd, subcommand);
                }
            }
            else
            {
                yield return new(cmd, null);
            }
        }
    }

    /// <summary>
    ///     Gets a command's object by name.
    /// </summary>
    /// <param name="commandName">Command to get.</param>
    /// <returns>A command object.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there is no command of the given name.</exception>
    public ToolshedCommand GetCommand(string commandName) => _commands[commandName];

    /// <summary>
    ///     Attempts to get a command's object by name.
    /// </summary>
    /// <param name="commandName">Command to get.</param>
    /// <param name="command">The command obtained, if any.</param>
    /// <returns>Success.</returns>
    public bool TryGetCommand(string commandName, [NotNullWhen(true)] out ToolshedCommand? command)
    {
        return _commands.TryGetValue(commandName, out command);
    }

    public ToolshedEnvironment(IEnumerable<Type> commands)
    {
        IoCManager.InjectDependencies(this);

        foreach (var ty in commands)
        {
            if (!ty.IsAssignableTo(typeof(ToolshedCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} was provided in a ToolshedEnvironment's constructor without being a child of {nameof(ToolshedCommand)}");
                continue;
            }

            var command = (ToolshedCommand)Activator.CreateInstance(ty)!;
            IoCManager.Resolve<IDependencyCollection>().InjectDependencies(command, oneOff: true);

            _commands.Add(command.Name, command);
        }

        InitializeQueries();
    }

    /// <summary>
    ///     Initializes a default toolshed context.
    /// </summary>
    public ToolshedEnvironment()
    {
        IoCManager.InjectDependencies(this);

        _log = _logManager.GetSawmill("toolshed");
        var watch = new Stopwatch();
        watch.Start();

        var tys = _reflection.FindTypesWithAttribute<ToolshedCommandAttribute>();
        foreach (var ty in tys)
        {
            if (!ty.IsAssignableTo(typeof(ToolshedCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(ToolshedCommandAttribute)} without being a child of {nameof(ToolshedCommand)}");
                continue;
            }

            var command = (ToolshedCommand)Activator.CreateInstance(ty)!;
            IoCManager.Resolve<IDependencyCollection>().InjectDependencies(command, oneOff: true);

            _commands.Add(command.Name, command);
        }

        InitializeQueries();

        _log.Info($"Initialized new toolshed context in {watch.Elapsed}");
    }

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
        foreach (var type in _toolshedManager.AllSteppedTypes(t))
        {
            var list = GetTypeImplList(type);
            if (type.IsGenericType)
            {
                list = Enumerable.Concat<CommandSpec>(list, GetTypeImplList(type.GetGenericTypeDefinition())).ToList();
            }
            foreach (var entry in list)
            {
                output.TryAdd((entry.Cmd.Name, entry.SubCommand), entry);
            }
        }

        return output.Values;
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
