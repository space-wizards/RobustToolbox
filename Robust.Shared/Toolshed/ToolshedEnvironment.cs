using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed class ToolshedEnvironment
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ToolshedManager _toolshedManager = default!;
    [Dependency] private readonly IDependencyCollection _dependency = default!;

    // Dictionary of commands, not including sub-commands
    private readonly Dictionary<string, ToolshedCommand> _commands = new();

    // All commands, including subcommands.
    private List<CommandSpec> _allCommands = new();

    private readonly Dictionary<Type, List<CommandSpec>> _commandTypeMap = new();
    private readonly Dictionary<Type, List<CommandSpec>> _commandPipeValueMap = new();
    private readonly Dictionary<CommandSpec, HashSet<Type>> _commandReturnValueMap = new();
    private readonly Dictionary<Type, CommandSpec[]> _commandTypeCache = new();
    private readonly Dictionary<Type, CompletionOption[]> _commandCompletionCache = new();

    private ISawmill _log = default!;

    /// <summary>
    ///     Provides every registered command, including subcommands.
    /// </summary>
    /// <returns>Enumerable of every command.</returns>
    public IReadOnlyList<CommandSpec> AllCommands()
    {
        return _allCommands;
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

    /// <summary>
    ///     Initializes a default toolshed context.
    /// </summary>
    public ToolshedEnvironment()
    {
        IoCManager.InjectDependencies(this);
        Init(_reflection.FindTypesWithAttribute<ToolshedCommandAttribute>());
    }

    /// <summary>
    /// Initialized a toolshed context with only the specified toolshed commands.
    /// </summary>
    public ToolshedEnvironment(IEnumerable<Type> commands)
    {
        IoCManager.InjectDependencies(this);
        Init(commands);
    }

    private void Init(IEnumerable<Type> commands)
    {
        _log = _logManager.GetSawmill("toolshed");
        var watch = new Stopwatch();
        watch.Start();

        foreach (var ty in commands)
        {
            if (!ty.IsAssignableTo(typeof(ToolshedCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(ToolshedCommandAttribute)} without being a child of {nameof(ToolshedCommand)}");
                continue;
            }

            var cmd = (ToolshedCommand)Activator.CreateInstance(ty)!;
            _dependency.InjectDependencies(cmd, oneOff: true);
            cmd.Init();
            _commands.Add(cmd.Name, cmd);

            var list = new List<CommandSpec>();
            _commandTypeMap.Add(ty, list);

            foreach (var impl in cmd.CommandImplementors.Values)
            {
                list.Add(impl.Spec);
                _allCommands.Add(impl.Spec);

                foreach (var method in impl.Methods)
                {
                    var piped = method.PipeArg?.ParameterType ?? typeof(void);

                    GetTypeImplList(piped).Add(impl.Spec);
                    var invList = GetCommandRetValuesInternal(impl.Spec);
                    if (cmd.TryGetReturnType(impl.SubCommand, piped, null, out var retType) || method.Info.ReturnType.Constructable())
                    {
                        invList.Add((retType ?? method.Info.ReturnType));
                    }
                }
            }
        }

        _log.Info($"Initialized new toolshed context in {watch.Elapsed}");
    }

    public bool TryGetCommands<T>([NotNullWhen(true)] out IReadOnlyList<CommandSpec>? commands)
        where T : ToolshedCommand
    {
        commands = null;
        if (!_commandTypeMap.TryGetValue(typeof(T), out var list))
            return false;

        commands = list;
        return true;
    }

    /// <summary>
    ///     Returns all commands that accept the given type.
    /// </summary>
    /// <param name="t">Type to use in the query.</param>
    /// <returns>Enumerable of matching command specs.</returns>
    /// <remarks>Not currently type constraint aware.</remarks>
    internal CommandSpec[] CommandsTakingType(Type? t)
    {
        t ??= typeof(void);
        if (_commandTypeCache.TryGetValue(t, out var arr))
            return arr;

        var output = new Dictionary<(string, string?), CommandSpec>();
        foreach (var type in _toolshedManager.AllSteppedTypes(t))
        {
            var list = GetTypeImplList(type);
            foreach (var entry in list)
            {
                output.TryAdd((entry.Cmd.Name, entry.SubCommand), entry);
            }

            if (!type.IsGenericType)
                continue;

            foreach (var entry in GetTypeImplList(type.GetGenericTypeDefinition()))
            {
                output.TryAdd((entry.Cmd.Name, entry.SubCommand), entry);
            }
        }

        return _commandTypeCache[t] = output.Values.ToArray();
    }

    // TODO TOOLSHED Fix CommandCompletionsForType
    // This fails to generate some completions. E.g., "i 1 iota iterate". It never generates the completions for
    // iterate, even though it takes in an unconstrained generic type. Note that this is just for completions, the
    // actual command executes fine. E.g.: "i 1 iota iterate { take 1 } 3" works as spected
    public CompletionResult CommandCompletionsForType(Type? t)
    {
        t ??= typeof(void);
        if (!_commandCompletionCache.TryGetValue(t, out var arr))
            arr = _commandCompletionCache[t] = CommandsTakingType(t).Select(x => x.AsCompletion()).ToArray();

        return CompletionResult.FromHintOptions(arr, "<command>");
    }

    public CompletionResult SubCommandCompletionsForType(Type? t, ToolshedCommand command)
    {
        // TODO TOOLSHED Cache this?
        // Maybe cache this or figure out some way to avoid having to iterate over unrelated commands.
        // I.e., restrict the iteration to only happen over subcommands.
        var cmds = CommandsTakingType(t)
            .Where(x => x.Cmd == command)
            .Select(x => x.AsCompletion());
        return CompletionResult.FromHintOptions(cmds, "<command>");
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
        return _commandReturnValueMap.GetOrNew(command);
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

        if (t is {IsGenericType: true, IsConstructedGenericType: false})
            t = t.GetGenericTypeDefinition();

        return _commandPipeValueMap.GetOrNew(t);
    }
}
