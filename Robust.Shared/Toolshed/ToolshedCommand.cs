using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

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
    public string Name { get; private set; } = default!;

    /// <summary>
    ///     Whether or not this command has subcommands.
    /// </summary>
    public bool HasSubCommands;

    /// <summary>
    ///     The additional type parameters of this command, specifically which parsers to use.
    /// </summary>
    /// <remarks>Every type specified must be either be <see cref="TypeTypeParser"/> or must inherit from CustomTypeParser&lt;Type&gt;.</remarks>
    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

    /// <summary>
    ///     The set of all subcommands on this command.
    /// </summary>
    public IEnumerable<string> Subcommands => CommandImplementors.Keys;

    internal readonly Dictionary<string, ToolshedCommandImplementor> CommandImplementors = new();

    private readonly Dictionary<string, HashSet<Type>> _acceptedTypes = new();

    protected internal ToolshedCommand()
    {
    }

    internal void Init()
    {
        var type = GetType();
        var name = type.GetCustomAttribute<ToolshedCommandAttribute>()!.Name;
        if (name is null)
        {
            var typeName = type.Name;
            const string commandStr = "Command";
            if (!typeName.EndsWith(commandStr))
                throw new InvalidCommandImplementation($"Command {type} must end with the word Command");

            name = typeName[..^commandStr.Length].ToLowerInvariant();
        }

        if (string.IsNullOrEmpty(name) || !name.EnumerateRunes().All(ParserContext.IsCommandToken))
            throw new InvalidCommandImplementation($"Command name contains invalid tokens");

        Name = name;

        foreach (var typeParser in TypeParameterParsers)
        {
            if (typeParser == typeof(TypeTypeParser))
                continue;
            if (!typeParser.IsAssignableTo(typeof(CustomTypeParser<Type>)))
                throw new InvalidCommandImplementation($"{nameof(TypeParameterParsers)} element {typeParser} is not {nameof(TypeTypeParser)} or assignable to {typeof(CustomTypeParser<Type>).PrettyName()}");
        }

        var impls = GetGenericImplementations().ToArray();
        if (impls.Length == 0)
            throw new Exception($"Command has no implementations?");

        var implementations = new HashSet<(string?, Type?)>();
        var argNames =  new HashSet<string>();
        var hasNonSubCommands = false;

        foreach (var impl in impls)
        {
            var hasInverted = false;
            var hasCtx = false;
            Type? pipeType = null;
            argNames.Clear();

            foreach (var param in impl.GetParameters())
            {
                var hasAnyAttribute = false;

                if (param.HasCustomAttribute<CommandArgumentAttribute>())
                {
                    if (param.Name == null || !argNames.Add(param.Name))
                        throw new InvalidCommandImplementation($"Command arguments must have a unique name");
                    hasAnyAttribute = true;
                    ValidateArg(param);
                }

                if (param.HasCustomAttribute<PipedArgumentAttribute>())
                {
                    if (hasAnyAttribute)
                        throw new InvalidCommandImplementation($"Method parameter cannot have more than one relevant attribute");
                    if (pipeType != null)
                        throw new InvalidCommandImplementation($"Commands cannot have more than one piped argument");
                    pipeType = param.ParameterType;
                    hasAnyAttribute = true;
                }

                if (param.HasCustomAttribute<CommandInvertedAttribute>())
                {
                    if (hasAnyAttribute)
                        throw new InvalidCommandImplementation($"Method parameter cannot have more than one relevant attribute");
                    if (hasInverted)
                        throw new InvalidCommandImplementation($"Duplicate {nameof(CommandInvertedAttribute)}");
                    if (param.ParameterType != typeof(bool))
                        throw new InvalidCommandImplementation($"Command argument with the {nameof(CommandInvertedAttribute)} must be of type bool");
                    hasInverted = true;
                    hasAnyAttribute = true;
                }

                if (param.HasCustomAttribute<CommandInvocationContextAttribute>())
                {
                    if (hasAnyAttribute)
                        throw new InvalidCommandImplementation($"Method parameter cannot have more than one relevant attribute");
                    if (hasCtx)
                        throw new InvalidCommandImplementation($"Duplicate {nameof(CommandInvocationContextAttribute)}");
                    if (param.ParameterType != typeof(IInvocationContext))
                        throw new InvalidCommandImplementation($"Command argument with the {nameof(CommandInvocationContextAttribute)} must be of type {nameof(IInvocationContext)}");
                    hasCtx = true;
                    hasAnyAttribute = true;
                }

                if (hasAnyAttribute)
                    continue;

                // Implicit [CommandInvocationContext]
                if (param.ParameterType == typeof(IInvocationContext))
                {
                    if (hasCtx)
                        throw new InvalidCommandImplementation($"Duplicate (implicit?) {nameof(CommandInvocationContextAttribute)}");
                    hasCtx = true;
                    continue;
                }

                // Implicit [CommandArgument]
                if (param.Name == null || !argNames.Add(param.Name))
                    throw new InvalidCommandImplementation($"Command arguments must have a unique name");
                ValidateArg(param);
            }

            var takesPipedGeneric = impl.HasCustomAttribute<TakesPipedTypeAsGenericAttribute>();
            var expected = TypeParameterParsers.Length + (takesPipedGeneric ? 1 : 0);
            var genericCount = impl.IsGenericMethodDefinition ? impl.GetGenericArguments().Length : 0;
            if (genericCount != expected)
                throw new InvalidCommandImplementation("Incorrect number of generic arguments.");

            if (takesPipedGeneric)
            {
                if (!impl.IsGenericMethodDefinition)
                    throw new InvalidCommandImplementation($"{nameof(TakesPipedTypeAsGenericAttribute)} requires a method to have generics");
                if (pipeType == null)
                    throw new InvalidCommandImplementation($"{nameof(TakesPipedTypeAsGenericAttribute)} required there to be a piped parameter");

                // type that would used to create a concrete method if the desired pipe type were passed in.
                var expectedGeneric = ToolshedCommandImplementor.GetGenericTypeFromPiped(pipeType, pipeType);
                var lastGeneric = impl.GetGenericArguments()[^1];
                if (expectedGeneric != lastGeneric)
                    throw new InvalidCommandImplementation($"Commands using {nameof(TakesPipedTypeAsGenericAttribute)} must have the inferred piped parameter type {expectedGeneric.Name} be the last generic parameter");
            }

            string? subCmd = null;
            if (impl.GetCustomAttribute<CommandImplementationAttribute>() is {SubCommand: { } x})
            {
                subCmd = x;
                HasSubCommands = true;
                if (string.IsNullOrEmpty(subCmd) || !subCmd.EnumerateRunes().All(ParserContext.IsToken))
                    throw new InvalidCommandImplementation($"Subcommand name {subCmd} contains invalid tokens");
            }
            else
            {
                hasNonSubCommands = true;
            }

            // Currently a command either has no subcommands, or **only** subcommands. This was the behaviour when I got
            // here, and I don't see a clear reason why it couldn't be supported if desired.
            if (hasNonSubCommands && HasSubCommands)
                throw new InvalidCommandImplementation("Toolshed commands either need to be all sub-commands, or have no sub commands at all.");

            // AFAIK this is currently just not supported, though it could eventually be added?
            if (!implementations.Add((subCmd, pipeType)))
                throw new InvalidCommandImplementation("The combination of subcommand and piped parameter type must be unique");

            var key = subCmd ?? string.Empty;
            if (!CommandImplementors.ContainsKey(key))
                CommandImplementors[key] = new ToolshedCommandImplementor(subCmd, this, Toolshed, Loc);
        }
    }

    private void ValidateArg(ParameterInfo arg)
    {
        var isParams = arg.HasCustomAttribute<ParamArrayAttribute>();
        if (!isParams)
            return;

        // I'm honestly not even sure if dotnet 9 collections use the same attribute, a quick search hasn't come
        // up with anything.
        if (!arg.ParameterType.IsArray)
            throw new InvalidCommandImplementation(".net 9 params collections are not yet supported");
    }

    internal HashSet<Type> AcceptedTypes(string? subCommand)
    {
        if (_acceptedTypes.TryGetValue(subCommand ?? "", out var set))
            return set;

        return _acceptedTypes[subCommand ?? ""] = GetType()
            .GetMethods(MethodFlags)
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>() is {} attr  && attr.SubCommand == subCommand )
            .Select(x => x.ConsoleGetPipedArgument())
            .Where(x => x != null)
            .Select(x => x!.ParameterType)
            .ToHashSet();
    }
}

internal sealed class CommandInvocationArguments
{
    public required object? PipedArgument;
    public required IInvocationContext Context { get; set; }
    public required CommandArgumentBundle Bundle;
    public Dictionary<string, object?>? Arguments => Bundle.Arguments;
    public bool Inverted => Bundle.Inverted;
}

/// <summary>
/// Collection of values used in the process of parsing a single command.
/// </summary>
public struct CommandArgumentBundle
{
    /// <summary>
    /// The name of the command currently being parsed.
    /// </summary>
    public string? Command;

    /// <summary>
    /// The name of the sub-command currently being parsed.
    /// </summary>
    public string? SubCommand;

    /// <summary>
    /// The collection of arguments that will be handed to the command method.
    /// </summary>
    public Dictionary<string, object?>? Arguments;

    /// <summary>
    /// The collection of type arguments that will be used to get a concrete method for generic commands.
    /// This does not include any generic parameters that are inferred from the <see cref="PipedType"/>.
    /// </summary>
    public Type[]? TypeArguments;

    /// <summary>
    /// The value that will get passed to any method arguments with the <see cref="CommandInvertedAttribute"/>.
    /// </summary>
    public required bool Inverted;

    /// <summary>
    /// The type of input that will be piped into this command.
    /// </summary>
    public required Type? PipedType;

    /// <summary>
    /// The index where the command's name starts. Used for contextualising errors.
    /// </summary>
    public int NameStart;

    /// <summary>
    /// The index where the (sub)command's name ends. Used for contextualising errors.
    /// </summary>
    public int NameEnd;
}

internal readonly record struct CommandDiscriminator(Type? PipedType, Type[]? TypeArguments)
{
    public bool Equals(CommandDiscriminator other)
    {
        if (other.PipedType != PipedType)
            return false;

        if (other.TypeArguments == null && TypeArguments == null)
            return true;

        if (TypeArguments == null)
            return false;

        if (TypeArguments.Length != other.TypeArguments!.Length)
            return false;

        return TypeArguments.SequenceEqual(other.TypeArguments);
    }

    public override int GetHashCode()
    {
        // poor man's hash do not judge
        var h = PipedType?.GetHashCode() ?? (int.MaxValue / 3);
        if (TypeArguments == null)
            return h;

        foreach (var arg in TypeArguments)
        {
            h += h ^ arg.GetHashCode();
            int.RotateLeft(h, 3);
        }

        return h;
    }
}

public sealed class InvalidCommandImplementation(string message) : Exception(message);
