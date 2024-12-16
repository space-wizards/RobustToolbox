using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Robust.Shared.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using Invocable = System.Func<Robust.Shared.Toolshed.CommandInvocationArguments, object?>;

namespace Robust.Shared.Toolshed;

internal sealed class ToolshedCommandImplementor
{
    public readonly ToolshedCommand Owner;
    public readonly string? SubCommand;

    public readonly string FullName;

    /// <summary>
    /// The full name of a command for use when fetching localized strings.
    /// </summary>
    public readonly string LocName;

    private readonly ToolshedManager _toolshed;
    private readonly ILocalizationManager _loc;
    public readonly Dictionary<CommandDiscriminator, Func<CommandInvocationArguments, object?>> Implementations = new();

    /// <summary>
    /// Cache for <see cref="TryGetConcreteMethod"/>.
    /// </summary>
    private readonly Dictionary<CommandDiscriminator, ConcreteCommandMethod?> _methodCache = new();

    /// <summary>
    /// All methods in <see cref="Owner"/> that correspond to the given <see cref="SubCommand"/>.
    /// </summary>
    internal readonly CommandMethod[] Methods;

    public CommandSpec Spec => new(Owner, SubCommand);

    public ToolshedCommandImplementor(string? subCommand, ToolshedCommand owner, ToolshedManager toolshed, ILocalizationManager loc)
    {
        Owner = owner;
        _loc = loc;
        SubCommand = subCommand;
        FullName = SubCommand == null ? Owner.Name : $"{Owner.Name}:{SubCommand}";
        _toolshed = toolshed;

        Methods = Owner.GetType()
            .GetMethods(ToolshedCommand.MethodFlags)
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>() is { } attr &&
                        attr.SubCommand == SubCommand)
            .Select(x => new CommandMethod(x))
            .ToArray();

        LocName = Owner.Name.All(char.IsAsciiLetterOrDigit)
            ? Owner.Name
            : Owner.GetType().PrettyName();

        if (SubCommand != null)
            LocName =  $"{LocName}-{SubCommand}";
    }

    /// <summary>
    /// Attempt to parse the type-arguments and arguments and return an invocable expression.
    /// </summary>
    public bool TryParse(ParserContext ctx, out Invocable? invocable, [NotNullWhen(true)] out ConcreteCommandMethod? method)
    {
        ctx.ConsumeWhitespace();
        method = null;
        invocable = null;

        if (!TryParseTypeArguments(ctx))
            return false;

        if (!TryGetConcreteMethod(ctx.Bundle.PipedType, ctx.Bundle.TypeArguments, out method))
        {
            if (!ctx.GenerateCompletions)
                ctx.Error = new NoImplementationError(ctx);
            return false;
        }

        var argsStart = ctx.Index;
        if (!TryParseArguments(ctx, method.Value))
        {
            ctx.Error?.Contextualize(ctx.Input, (argsStart, ctx.Index));
            return false;
        }

        invocable = GetImplementation(ctx.Bundle, method.Value);
        return true;
    }

    /// <summary>
    ///     You who tread upon this dreaded land, what is it that brings you here?
    ///     For this place is not for you, this land of terror and death.
    ///     It brings fear to all who tread within, terror to the homes ahead.
    ///     Begone, foul maintainer, for this place is not for thee.
    /// </summary>
    public bool TryParseArguments(ParserContext ctx, ConcreteCommandMethod method)
    {
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);

        ref var args = ref ctx.Bundle.Arguments;
        foreach (var arg in method.Args)
        {
            if (!TryParseArgument(ctx, arg, ref args))
                return false;
        }

        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        return true;
    }

    private bool TryParseArgument(ParserContext ctx, CommandArgument arg, ref Dictionary<string, object?>? args)
    {
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        var start = ctx.Index;
        var save = ctx.Save();
        ctx.ConsumeWhitespace();

        if (ctx.PeekCommandOrBlockTerminated() || ctx is {OutOfInput: true, GenerateCompletions: false})
        {
            ctx.Error = new ExpectedArgumentError(arg.Type);
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
            return false;
        }

        if (!arg.Parser.TryParse(ctx, out var parsed))
        {
            if (ctx.GenerateCompletions)
            {
                // Dont generate completions for the end of a command for an error that occured early on.
                // I.e., "i asd " should not be suggesting people to enter an integer.
                if (!ctx.OutOfInput)
                    return false;

                // Some parsers might already generate completions when they fail the initial parsing.
                if (ctx.Completions != null)
                    return false;

                ctx.Restore(save);
                ctx.Error = null;
                ctx.Completions ??= arg.Parser.TryAutocomplete(ctx, arg.Name);
                TrySetArgHint(ctx, arg.Name);
                return false;
            }

            // un-parseable types don't even consume input / modify the index
            // However for contextualizing the error, we want to at least show where the failing argument was
            var end = Math.Max(start + 1, ctx.Index);

            ctx.Error ??= new ArgumentParseError(arg.Type, arg.Parser.GetType());
            ctx.Error.Contextualize(ctx.Input, (start, end));
            return false;
        }

        // All arguments should have been parsed as a ValueRef<T> or Block, unless this is using some custom type parser
#if DEBUG
        var t = parsed.GetType();
        if (arg.Parser.GetType().IsCustomParser())
        {
            DebugTools.Assert(t.IsAssignableTo(arg.Type)
                              || t.IsAssignableTo(typeof(Block))
                              || t.IsValueRef());
        }
        else if (arg.Type.IsAssignableTo(typeof(Block)))
            DebugTools.Assert(t.IsAssignableTo(typeof(Block)));
        else
            DebugTools.Assert(t.IsValueRef());
#endif

        args ??= new();
        args[arg.Name] = parsed;

        if (!ctx.GenerateCompletions || !ctx.OutOfInput)
            return true;

        // This was the end of the input, so we want to get completions for the current argument, not the next
        // argument. I.e., if we started writing out a variable name, we want to keep generating variable name
        // suggestions for the current argument. This is true even if the current string corresponds to a valid
        // variable.

        ctx.Restore(save);
        ctx.Error = null;
        ctx.Completions ??= arg.Parser.TryAutocomplete(ctx, arg.Name);
        TrySetArgHint(ctx, arg.Name);

        // TODO TOOLSHED invalid-fail
        // This can technically "fail" to parse a valid command, however this only happens when generating
        // completions, not when actually executing the command. Still, this is pretty janky and I don't know of a
        // good fix.
        return false;
    }


    private void TrySetArgHint(ParserContext ctx, string argName)
    {
        if (ctx.Completions == null)
            return;

        if (_loc.TryGetString($"command-arg-hint-{LocName}-{argName}", out var hint))
            ctx.Completions.Hint = hint;
    }

    internal bool TryParseTypeArguments(ParserContext ctx)
    {
        if (Owner.TypeParameterParsers.Length == 0)
            return true;

        ref var typeArguments = ref ctx.Bundle.TypeArguments;
        typeArguments = new Type[Owner.TypeParameterParsers.Length];

        for (var i = 0; i < Owner.TypeParameterParsers.Length; i++)
        {
            DebugTools.AssertNull(ctx.Error);
            DebugTools.AssertNull(ctx.Completions);
            var parserType = Owner.TypeParameterParsers[i];
            var start = ctx.Index;
            ctx.ConsumeWhitespace();
            var save = ctx.Save();

            if (ctx is {OutOfInput: true, GenerateCompletions: false} || ctx.PeekCommandOrBlockTerminated())
            {
                ctx.Error = new ExpectedTypeArgumentError();
                ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
                return false;
            }

            var parser = (BaseParser<Type>) (parserType == typeof(TypeTypeParser)
                ? _toolshed.GetParserForType(typeof(Type))!
                : _toolshed.GetCustomParser(parserType));

            DebugTools.AssertNull(ctx.Completions);
            if (!parser.TryParse(ctx, out var parsed))
            {
                typeArguments = null;
                if (ctx.GenerateCompletions)
                {
                    // Dont generate completions for the end of a command for an error that occured early on.
                    // I.e., "i asd " should not be suggesting people to enter an integer.
                    if (!ctx.OutOfInput)
                        return false;

                    ctx.Restore(save);
                    ctx.Error = null;
                    ctx.Completions ??= parser.TryAutocomplete(ctx, null);
                    return false;
                }

                ctx.Error ??= new TypeArgumentParseError(parserType);
                ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
                return false;
            }

            typeArguments[i] = parsed;

            if (!ctx.GenerateCompletions || !ctx.OutOfInput)
                continue;

            // This was the end of the input, so we want to get completions for the current argument, not the next
            // argument. I.e., if we started writing out the name of a type, we want to keep generating type name
            // suggestions for the current argument. This is true even if the current string already corresponds to a
            // valid type.

            ctx.Restore(save);
            ctx.Error = null;
            ctx.Completions = parser.TryAutocomplete(ctx, null);

            // TODO TOOLSHED invalid-fail
            // This can technically "fail" to parse a valid command, however this only happens when generating
            // completions, not when actually executing the command. Still, this is pretty janky and I don't know of a
            // good fix.
            return false;
        }

        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        return true;
    }

    /// <summary>
    /// Attempt to get a concrete method that takes in the given generic type arguments.
    /// </summary>
    internal bool TryGetConcreteMethod(
        Type? pipedType,
        Type[]? typeArguments,
        [NotNullWhen(true)] out ConcreteCommandMethod? method)
    {
        var idx = new CommandDiscriminator(pipedType, typeArguments);
        if (_methodCache.TryGetValue(idx, out method))
            return method != null;

        var result = GetConcreteMethodInternal(pipedType, typeArguments);
        if (result == null)
        {
            _methodCache[idx] = method = null;
            return false;
        }

        var (cmd, info) = result.Value;
        if (pipedType is {ContainsGenericParameters: true} || typeArguments != null && typeArguments.Any(x => x.ContainsGenericParameters))
        {
            // I hate this method name
            // its not a real concrete method if the requested types are generic, is it now?
            // anyways, fuck this I CBF fixing it just return without information about the arguments.
            _methodCache[idx] = method = new(info, default!, cmd);
            return true;
        }

        var args = info.GetParameters()
            .Where(x => x.IsCommandArgument())
            .Select(x => new CommandArgument(x.Name!, x.ParameterType, GetArgumentParser(x)))
            .ToArray();

        _methodCache[idx] = method = new(info, args, cmd);
        return true;
    }

    private ITypeParser GetArgumentParser(ParameterInfo param)
    {
        var attrib = param.GetCustomAttribute<CommandArgumentAttribute>();
        var parser = attrib?.CustomParser is not {} custom
            ? _toolshed.GetArgumentParser(param.ParameterType)
            : _toolshed.GetArgumentParser(_toolshed.GetCustomParser(custom));

        if (parser == null)
            throw new Exception($"No parser for type: {param.ParameterType}");
        return parser;
    }

    private (CommandMethod, MethodInfo)? GetConcreteMethodInternal(Type? pipedType, Type[]? typeArguments)
    {
        return Methods
            .Where(x =>
            {
                if (x.PipeArg is not { } param)
                    return pipedType is null;

                if (pipedType == null)
                    return false; // We want exact match to be preferred!

                return x.Generic || _toolshed.IsTransformableTo(pipedType, param.ParameterType);

                // Finally, prefer specialized (type exact) implementations.
            })
            .OrderByDescending(x =>
            {
                if (x.PipeArg is not { } param)
                    return 0;

                if (pipedType!.IsAssignableTo(param.ParameterType))
                    return 1000; // We want exact match to be preferred!
                if (param.ParameterType.GetMostGenericPossible() == pipedType.GetMostGenericPossible())
                    return 500; // If not, try to prefer the same base type.

                // Finally, prefer specialized (type exact) implementations.
                return param.ParameterType.IsGenericTypeParameter ? 0 : 100;

            })
            .Select(x =>
            {
                if (!x.Generic)
                    return ((CommandMethod, MethodInfo)?)(x, x.Info);

                try
                {
                    if (!x.PipeGeneric)
                        return (x, x.Info.MakeGenericMethod(typeArguments!));

                    var t = GetGenericTypeFromPiped(pipedType!, x.PipeArg!.ParameterType);
                    return (x, x.Info.MakeGenericMethod(typeArguments?.Append(t).ToArray() ?? [t]));
                }
                catch (ArgumentException)
                {
                    return null;
                }
            })
            .FirstOrDefault(x => x != null);
    }

    /// <summary>
    /// When a method has the <see cref="TakesPipedTypeAsGenericAttribute"/>, this method is used to actually
    /// determine the generic type argument given the type of the piped in value.
    /// </summary>
    /// <param name="inputType">The type of value that was piped in</param>
    /// <param name="parameterType">The type as specified in the method</param>
    public static Type GetGenericTypeFromPiped(Type inputType, Type parameterType)
    {
        // inputType!.IntersectWithGeneric(parameterType, _toolshed, true);

        // I don't really understand the logic behind this
        // Actually I understand it now, but its just broken or incomplete. Yipeee
        return inputType.Intersect(parameterType);
}

    /// <summary>
    ///     Attempts to fetch a callable shim for a command, aka it's implementation, using the given types.
    /// </summary>
    public Func<CommandInvocationArguments, object?> GetImplementation(CommandArgumentBundle args, ConcreteCommandMethod method)
    {
        var dis = new CommandDiscriminator(args.PipedType, args.TypeArguments);
        if (!Implementations.TryGetValue(dis, out var impl))
            Implementations[dis] = impl = GetImplementationInternal(args, method);

        return impl;
    }

    internal Func<CommandInvocationArguments, object?> GetImplementationInternal(CommandArgumentBundle cmdArgs, ConcreteCommandMethod method)
    {
        var args = Expression.Parameter(typeof(CommandInvocationArguments));
        var paramList = new List<Expression>();

        foreach (var param in method.Info.GetParameters())
        {
            paramList.Add(GetParamExpr(param, cmdArgs.PipedType, args));
        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), method.Info, paramList);

        var returnType = method.Info.ReturnType;
        if (returnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));
        else if (returnType.IsValueType)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        return Expression.Lambda<Func<CommandInvocationArguments, object?>>(partialShim, args).Compile();
    }

    private Expression GetParamExpr(ParameterInfo param, Type? pipedType, ParameterExpression args)
    {
        if (param.HasCustomAttribute<PipedArgumentAttribute>())
        {
            if (pipedType is null)
                throw new TypeArgumentException();

            // (ParameterType)(args.PipedArgument)
            return _toolshed.GetTransformer(pipedType, param.ParameterType, Expression.Field(args, nameof(CommandInvocationArguments.PipedArgument)));
        }

        if (param.HasCustomAttribute<CommandInvertedAttribute>())
        {
            // args.Inverted
            return Expression.Property(args, nameof(CommandInvocationArguments.Inverted));
        }

        if (param.HasCustomAttribute<CommandArgumentAttribute>())
            return GetArgExpr(param, args);

        if (param.HasCustomAttribute<CommandInvocationContextAttribute>()
            || param.ParameterType == typeof(IInvocationContext))
        {
            // args.Context
            return Expression.Property(args, nameof(CommandInvocationArguments.Context));
        }

        // Implicit CommandArgumentAttribute
        return GetArgExpr(param, args);
    }

    private Expression GetArgExpr(ParameterInfo param, ParameterExpression args)
    {
        // args.Arguments[param.Name]
        var argValue = Expression.MakeIndex(
            Expression.Property(args, nameof(CommandInvocationArguments.Arguments)),
            typeof(Dictionary<string, object?>).FindIndexerProperty(),
            new[] {Expression.Constant(param.Name)});

        // args.Context
        var ctx = Expression.Property(args, nameof(CommandInvocationArguments.Context));

        // ValueRef<T>.TryEvaluate
        var evalMethod = typeof(ValueRef<>)
            .MakeGenericType(param.ParameterType)
            .GetMethod(nameof(ValueRef<int>.EvaluateParameter), BindingFlags.Static | BindingFlags.NonPublic)!;

        // ValueRef<T>.TryEvaluate(args.Arguments[param.Name], args.Context)
        return Expression.Call(evalMethod, argValue, ctx);
    }

    public override string ToString()
    {
        return FullName;
    }

    /// <inheritdoc cref="ToolshedCommand.GetHelp"/>
    public string GetHelp()
    {
        if (_loc.TryGetString($"command-help-{LocName}", out var str))
            return str;

        var builder = new StringBuilder();

        // If any of the commands are invertible via the "not" prefix, we point that out in the help string
        if (Methods.Any(x => x.Invertible))
            builder.AppendLine(_loc.GetString($"command-help-invertible"));

        // List usages by just printing all methods & their arguments
        builder.Append(_loc.GetString("command-help-usage"));
        foreach (var method in Methods)
        {
            builder.Append(Environment.NewLine + "  ");

            if (method.PipeArg != null)
                builder.Append($"<{method.PipeArg.Name} ({GetFriendlyName(method.PipeArg.ParameterType)})> -> ");

            if (method.Invertible)
                builder.Append("[not] ");

            builder.Append(FullName);

            foreach (var (argName, argType) in method.Arguments)
            {
                builder.Append($" <{argName} ({GetFriendlyName(argType)})>");
            }

            if (method.Info.ReturnType != typeof(void))
                builder.Append($" -> {GetFriendlyName(method.Info.ReturnType)}");
        }

        return builder.ToString();
    }

    /// <inheritdoc cref="ToolshedCommand.DescriptionLocKey"/>
    public string DescriptionLocKey() => $"command-description-{LocName}";

    /// <inheritdoc cref="ToolshedCommand.Description"/>
    public string Description()
    {
        return _loc.GetString(DescriptionLocKey());
    }

    public static string GetFriendlyName(Type type)
    {
        var friendlyName = type.Name;
        if (!type.IsGenericType)
            return friendlyName;

        var iBacktick = friendlyName.IndexOf('`');
        if (iBacktick > 0)
            friendlyName = friendlyName.Remove(iBacktick);

        friendlyName += "<";
        var typeParameters = type.GetGenericArguments();
        for (var i = 0; i < typeParameters.Length; ++i)
        {
            var typeParamName = GetFriendlyName(typeParameters[i]);
            friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
        }
        friendlyName += ">";

        return friendlyName;
    }
}


/// <summary>
/// Struct for caching information about a command's methods. Helps reduce LINQ & reflection calls when attempting
/// to find matching methods.
/// </summary>
internal sealed class CommandMethod
{
    /// <summary>
    /// The method associated with some command.
    /// </summary>
    public readonly MethodInfo Info;

    /// <summary>
    /// The argument associated with the piped value.
    /// </summary>
    public readonly ParameterInfo? PipeArg;

    public readonly bool Generic;
    public readonly bool Invertible;

    /// <summary>
    /// Whether the type of the piped value should be used as one of the type parameters for generic methods.
    /// I.e., whether the method has a <see cref="TakesPipedTypeAsGenericAttribute"/>.
    /// </summary>
    public readonly bool PipeGeneric;

    public readonly (string, Type)[] Arguments;

    public CommandMethod(MethodInfo info)
    {
        Info = info;
        PipeArg = info.ConsoleGetPipedArgument();
        Invertible = info.ConsoleHasInvertedArgument();

        Arguments = info.GetParameters()
            .Where(x => x.IsCommandArgument())
            .Select(x => (x.Name ?? string.Empty, x.ParameterType))
            .ToArray();

        if (!info.IsGenericMethodDefinition)
            return;

        Generic = true;
        PipeGeneric = info.HasCustomAttribute<TakesPipedTypeAsGenericAttribute>();
    }
}

internal readonly record struct ConcreteCommandMethod(MethodInfo Info, CommandArgument[] Args, CommandMethod Base);
internal readonly record struct CommandArgument(string Name, Type Type, ITypeParser Parser);

public sealed class ArgumentParseError(Type type, Type parser) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Failed to parse command argument of type {type.PrettyName()} using parser {parser.PrettyName()}");
    }
}

public sealed class ExpectedArgumentError(Type type) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Expected command argument of type {type.PrettyName()}, but ran out of input");
    }
}

public sealed class TypeArgumentParseError(Type parser) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Failed to parse type argument using parser {parser.PrettyName()}");
    }
}

public sealed class ExpectedTypeArgumentError : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Expected type argument, but ran out of input");
    }
}
