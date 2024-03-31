using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed;

/// <summary>
///     This class is used for implementing new commands in Toolshed.
/// </summary>
/// <remarks>
///     Toolshed's code generation will automatically handle creating command executor stubs, you don't need to override anything.
/// </remarks>
/// <example><code>
///     [ToolshedCommand]
///     public sealed class ExampleCommand : ToolshedCommand
///     {
///         // Toolshed will automatically infer autocompletion information, type information, and parsing.
///         [CommandImplementation]
///         public IEnumerable&lt;EntityUid&gt; Example(
///                 [PipedArgument] IEnumerable&lt;EntityUid&gt; input,
///                 [CommandArgument] int amount
///             )
///         {
///             return input.Take(amount);
///         }
///     }
/// </code></example>
/// <seealso cref="ToolshedManager"/>
/// <seealso cref="ToolshedCommandAttribute"/>
/// <seealso cref="CommandImplementationAttribute"/>
/// <seealso cref="PipedArgumentAttribute"/>
/// <seealso cref="CommandArgumentAttribute"/>
/// <seealso cref="CommandInvertedAttribute"/>
/// <seealso cref="CommandInvocationContextAttribute"/>
/// <seealso cref="TakesPipedTypeAsGenericAttribute"/>
[Reflect(false)]
public abstract partial class ToolshedCommand
{
    [Dependency] protected readonly ToolshedManager Toolshed = default!;
    [Dependency] protected readonly ILocalizationManager Loc = default!;

    /// <summary>
    ///     The user-facing name of the command.
    /// </summary>
    /// <remarks>This is automatically generated based on the type name unless overridden with <see cref="ToolshedCommandAttribute"/>.</remarks>
    public string Name { get; }

    /// <summary>
    ///     Whether or not this command has subcommands.
    /// </summary>
    public bool HasSubCommands { get; }

    /// <summary>
    ///     The additional type parameters of this command, specifically which parsers to use.
    /// </summary>
    /// <remarks>Every type specified must either be <see cref="Type"/> itself or something implementing <see cref="IAsType{Type}"/> where T is Type.</remarks>
    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

    internal bool HasTypeParameters => TypeParameterParsers.Length != 0;

    /// <summary>
    ///     The list of all subcommands on this command.
    /// </summary>
    public IEnumerable<string> Subcommands => _implementors.Keys.Where(x => x != "");

    protected ToolshedCommand()
    {
        var name = GetType().GetCustomAttribute<ToolshedCommandAttribute>()!.Name;

        if (name is null)
        {
            var typeName = GetType().Name;
            const string commandStr = "Command";

            if (!typeName.EndsWith(commandStr))
            {
                throw new InvalidComponentNameException($"Command {GetType()} must end with the word Command");
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
        Dictionary<(string, Type?), SortedDictionary<string, Type>> parameters = new();

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

            Type? pipedType = null;
            foreach (var param in impl.GetParameters())
            {
                if (param.GetCustomAttribute<CommandArgumentAttribute>() is not null)
                    myParams.TryAdd(param.Name!, param.ParameterType);

                if (param.GetCustomAttribute<PipedArgumentAttribute>() is not null)
                {
                    if (pipedType != null)
                        throw new NotSupportedException($"Commands cannot have more than one piped argument");
                    pipedType = param.ParameterType;
                }
            }

            var key = (subCmd ?? "", pipedType);
            if (parameters.TryAdd(key, myParams))
                continue;

            if (!parameters[key].SequenceEqual(myParams))
                throw new NotImplementedException("All command implementations of a given subcommand with the same pipe type must share the same argument types");
        }
    }

    internal IEnumerable<Type> AcceptedTypes(string? subCommand)
    {
        return GetGenericImplementations()
            .Where(x =>
                x.ConsoleGetPipedArgument() is not null
            &&  x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .Select(x => x.ConsoleGetPipedArgument()!.ParameterType);
    }

    internal bool TryParseArguments(
            bool doAutocomplete,
            ParserContext parserContext,
            Type? pipedType,
            string? subCommand,
            [NotNullWhen(true)] out Dictionary<string, object?>? args,
            out Type[] resolvedTypeArguments,
            out IConError? error,
            out ValueTask<(CompletionResult?, IConError?)>? autocomplete
        )
    {

        return _implementors[subCommand ?? ""].TryParseArguments(doAutocomplete, parserContext, subCommand, pipedType, out args, out resolvedTypeArguments, out error, out autocomplete);
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

internal readonly record struct CommandDiscriminator(Type? PipedType, Type[] TypeArguments)
{
    public bool Equals(CommandDiscriminator other)
    {
        return other.PipedType == PipedType && other.TypeArguments.SequenceEqual(TypeArguments);
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
