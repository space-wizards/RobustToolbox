using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

[Reflect(false)]
public abstract partial class ToolshedCommand
{
    [Dependency] protected readonly ToolshedManager Toolshed = default!;

    public string Name { get; }

    public bool HasSubCommands { get; }

    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

    public bool HasTypeParameters => TypeParameterParsers.Length != 0;

    public IEnumerable<string> Subcommands => _implementors.Keys.Where(x => x != "");

    public ToolshedCommand()
    {
        var name = GetType().GetCustomAttribute<ToolshedCommandAttribute>()!.Name;

        if (name is null)
        {
            var typeName = GetType().Name;
            const string commandStr = "Command";

            if (!typeName.EndsWith(commandStr))
            {
                throw new InvalidComponentNameException($"Component {GetType()} must end with the word Component");
            }

            name = typeName[..^commandStr.Length].ToLowerInvariant();
        }

        Name = name;
        HasSubCommands = false;
        _implementors[""] =
            new ToolshedCommandImplementor
            {
                Owner = this,
                SubCommand = null
            };

        var impls = GetGenericImplementations();
        Dictionary<string, SortedDictionary<string, Type>> parameters = new();

        foreach (var impl in impls)
        {
            var myParams = new SortedDictionary<string, Type>();
            string? subCmd = null;
            if (impl.GetCustomAttribute<CommandImplementationAttribute>() is {SubCommand: { } x})
            {
                subCmd = x;
                HasSubCommands = true;
                _implementors[x] =
                    new ToolshedCommandImplementor
                    {
                        Owner = this,
                        SubCommand = x
                    };
            }

            foreach (var param in impl.GetParameters())
            {
                if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
                {
                    if (parameters.ContainsKey(param.Name!))
                        continue;

                    myParams.Add(param.Name!, param.ParameterType);
                }
            }

            if (parameters.TryGetValue(subCmd ?? "", out var existing))
            {
                if (!existing.SequenceEqual(existing))
                {
                    throw new NotImplementedException("All command implementations of a given subcommand must share the same parameters!");
                }
            }
            else
                parameters.Add(subCmd ?? "", myParams);

        }
    }


    public IEnumerable<Type> AcceptedTypes(string? subCommand)
    {
        return GetGenericImplementations()
            .Where(x =>
                x.ConsoleGetPipedArgument() is not null
            &&  x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .Select(x => x.ConsoleGetPipedArgument()!.ParameterType);
    }

    internal bool TryParseArguments(bool doAutocomplete, ForwardParser parser, Type? pipedType, string? subCommand, [NotNullWhen(true)] out Dictionary<string, object?>? args, out Type[] resolvedTypeArguments, out IConError? error, out ValueTask<(CompletionResult?, IConError?)>? autocomplete)
    {
        return _implementors[subCommand ?? ""].TryParseArguments(doAutocomplete, parser, subCommand, pipedType, out args, out resolvedTypeArguments, out error, out autocomplete);
    }
}

internal sealed class CommandInvocationArguments
{
    public required object? PipedArgument;
    public required IInvocationContext Context { get; set; }
    public required CommandArgumentBundle Bundle;
    public Dictionary<string, object?> Arguments => Bundle.Arguments;
    public bool Inverted => Bundle.Inverted;
    public Type? PipedArgumentType => Bundle.PipedArgumentType;
}

internal sealed class CommandArgumentBundle
{
    public required Dictionary<string, object?> Arguments;
    public required bool Inverted = false;
    public required Type? PipedArgumentType;
    public required Type[] TypeArguments;
}

public readonly record struct CommandDiscriminator(Type? PipedType, Type[] TypeArguments) : IEquatable<CommandDiscriminator?>
{
    public bool Equals(CommandDiscriminator? other)
    {
        if (other is not {} value)
            return false;

        return value.PipedType == PipedType && value.TypeArguments.SequenceEqual(TypeArguments);
    }

    public override int GetHashCode()
    {
        // poor man's hash do not judge
        var h = PipedType?.GetHashCode() ?? (int.MaxValue / 3);
        foreach (var arg in TypeArguments)
        {
            h += h ^ arg.GetHashCode();
            int.RotateLeft(h, 3);
        }

        return h;
    }
}
