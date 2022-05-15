using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.ReflectionCommands;

public sealed class CommandManager
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    delegate void Command(IConsoleShell shell, string[] args);

    private readonly Dictionary<string, Command> _commands = new();

    [RegisterCommand("test")]
    public void Test(IConsoleShell shell, EntityUid uid, Color color, ResourcePath path)
    {
        shell.WriteLine($"{uid} | {color} | {path}");
    }

    [RegisterCommand("test_static")]
    public static void TestStatic(IConsoleShell shell, EntityUid uid, Color color, ResourcePath path)
    {
        shell.WriteLine($"{uid} | {color} | {path}");
    }

    public void Initialize()
    {
        RegisterCommand(typeof(CommandManager).GetMethod("Test")!, instance:this);
        RegisterCommand(typeof(CommandManager).GetMethod("TestStatic")!);

        var shell = new ConsoleShell(IoCManager.Resolve<IConsoleHost>(), null);
        ExecuteCommand(shell, "test 1 red path");
        ExecuteCommand(shell, "test_static 1 red path");
    }

    public void RegisterCommand(MethodInfo info, RegisterCommandAttribute? attribute = null, object? instance = null)
    {
        if (attribute == null && !info.TryGetCustomAttribute(out attribute))
        {
            throw new InvalidOperationException(
                $"{nameof(MethodInfo)} is not annotated with {nameof(RegisterCommandAttribute)}.");
        }

        if (_commands.ContainsKey(attribute.Command))
            throw new InvalidOperationException($"Command {attribute.Command} has been defined twice.");

        var parameters = info.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(IConsoleShell))
            throw new InvalidOperationException($"The first parameter should be typed as {nameof(IConsoleShell)}.");

        var serv3Const = Expression.Constant(_serializationManager);

        var shellParam = Expression.Parameter(typeof(IConsoleShell), "shell");
        var argsParam = Expression.Parameter(typeof(string[]), "args");

        var args = new Expression[parameters.Length];
        args[0] = shellParam;
        var valueDataNodeCtor = typeof(ValueDataNode).GetConstructor(new[] { typeof(string) })!;
        var nullConst = Expression.Default(typeof(ISerializationContext));
        var falseConst = Expression.Constant(false);

        for (int i = 1; i < args.Length; i++)
        {
            var readMethod = typeof(ISerializationManager).GetMethods().First(x => x.ContainsGenericParameters && x.Name == "Read").MakeGenericMethod(parameters[i].ParameterType);
            args[i] = Expression.Call(serv3Const, readMethod,
                Expression.New(valueDataNodeCtor, Expression.ArrayIndex(argsParam, Expression.Constant(i))), nullConst, falseConst, Expression.Default(parameters[i].ParameterType));
        }

        var call = instance == null ? Expression.Call(info, args) : Expression.Call(Expression.Constant(instance), info, args);

        var tree = Expression.Condition(Expression.Equal(Expression.ArrayLength(argsParam), Expression.Constant(args.Length)),
            call,
            Expression.Throw(Expression.New(typeof(InvalidOperationException))));

        _commands.Add(attribute.Command, Expression.Lambda<Command>(tree, new[] { shellParam, argsParam }).Compile());
    }

    public void ExecuteCommand(IConsoleShell shell, string command)
    {
        var args = new List<string>();
        CommandParsing.ParseArguments(command, args);

        var cmdName = args[0];
        if (!_commands.TryGetValue(cmdName, out var commandDelegate))
            throw new InvalidOperationException($"Command {cmdName} not found.");

        commandDelegate(shell, args.ToArray());
    }
}
