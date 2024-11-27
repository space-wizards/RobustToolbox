using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

internal sealed class ToolshedCommandImplementor
{
    public readonly ToolshedCommand Owner;
    public readonly string? SubCommand;

    public readonly string FullName;
    private readonly string _argHintKey;

    private readonly ToolshedManager _toolshed;
    private readonly ILocalizationManager _loc;
    public readonly Dictionary<CommandDiscriminator, (Func<CommandInvocationArguments, object?> Shim, Type ReturnType)?> Implementations = new();

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

        _argHintKey = $"command-arg-hint-{Owner.GetLocKeyName(subCommand)}-";
    }

    /// <summary>
    ///     You who tread upon this dreaded land, what is it that brings you here?
    ///     For this place is not for you, this land of terror and death.
    ///     It brings fear to all who tread within, terror to the homes ahead.
    ///     Begone, foul maintainer, for this place is not for thee.
    /// </summary>
    public bool TryParseArguments(ParserContext ctx)
    {
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        var firstStart = ctx.Index;

        if (!TryParseTypeArguments(ctx))
            return false;

        if (!TryGetConcreteMethod(ctx.Bundle.PipedType, ctx.Bundle.TypeArguments, out var impl))
        {
            if (ctx.GenerateCompletions)
                return false;

            ctx.Error = new NoImplementationError(ctx);
            ctx.Error.Contextualize(ctx.Input, (firstStart, ctx.Index));
            return false;
        }

        ref var args = ref ctx.Bundle.Arguments;
        foreach (var arg in impl.Value.Args)
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

        if (_loc.TryGetString($"{_argHintKey}{argName}", out var hint))
            ctx.Completions.Hint = hint;
    }

    private bool TryParseTypeArguments(ParserContext ctx)
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

        var info = GetConcreteMethodInternal(pipedType, typeArguments);
        if (info == null)
        {
            _methodCache[idx] = method = null;
            return false;
        }

        if (pipedType is {ContainsGenericParameters: true} || typeArguments != null && typeArguments.Any(x => x.ContainsGenericParameters))
        {
            // I hate this method name
            // its not a real concrete method if the requested types are generic, is it now?
            // anyways, fuck this I CBF fixing it just return without information about the arguments.
            _methodCache[idx] = method = new(info, default!);
            return true;
        }

        var args = info.GetParameters()
            .Where(IsCommandArgument)
            .Select(x => (x, x.GetCustomAttribute<CommandArgumentAttribute>()))
            .Select(x =>
            {
                var parser = x.Item2?.CustomParser is not {} custom
                    ? _toolshed.GetArgumentParser(x.Item1.ParameterType)
                    : _toolshed.GetArgumentParser(_toolshed.GetCustomParser(custom));

                if (parser == null)
                    throw new Exception($"No parser for type: {x.Item1.ParameterType}");
                return new CommandArgument(x.Item1.Name!, x.Item1.ParameterType, parser);

            })
            .ToArray();

        _methodCache[idx] = method = new(info, args);
        return true;
    }

    private bool IsCommandArgument(ParameterInfo param)
    {
        if (param.HasCustomAttribute<CommandArgumentAttribute>())
            return true;

        if (param.HasCustomAttribute<CommandInvertedAttribute>())
            return false;

        if (param.HasCustomAttribute<PipedArgumentAttribute>())
            return false;

        if (param.HasCustomAttribute<CommandInvocationContextAttribute>())
            return false;

        return param.ParameterType != typeof(IInvocationContext);
    }

    private MethodInfo? GetConcreteMethodInternal(Type? pipedType, Type[]? typeArguments)
    {
        var impls = Methods
            .Where(x =>
            {
                if (x.PipeArg is not { } param)
                    return pipedType is null;

                if (pipedType == null)
                    return false;

                return x.IsGeneric || _toolshed.IsTransformableTo(pipedType, param.ParameterType);
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
                if (!x.IsGeneric)
                    return x.Info;

                try
                {
                    if (!x.PipeGeneric)
                        return x.Info.MakeGenericMethod(typeArguments!);

                    var t = GetGenericTypeFromPiped(pipedType!, x.PipeArg!.ParameterType);
                    return x.Info.MakeGenericMethod(typeArguments?.Append(t).ToArray() ?? [t]);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            })
            .Where(x => x != null);

        // ReSharper disable once PossibleMultipleEnumeration
        return impls.FirstOrDefault();
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
        return inputType.Intersect(parameterType);
}

    /// <summary>
    ///     Attempts to fetch a callable shim for a command, aka it's implementation, using the given types.
    /// </summary>
    public bool TryGetInvokable(
        ParserContext ctx,
        [NotNullWhen(true)] out Func<CommandInvocationArguments, object?>? func,
        [NotNullWhen(true)] out Type? returnType)
    {
        var dis = new CommandDiscriminator(ctx.Bundle.PipedType, ctx.Bundle.TypeArguments);
        if (!Implementations.TryGetValue(dis, out var impl))
            Implementations[dis] = impl = GetInvokableInternal(ctx);

        if (impl == null)
        {
            func = null;
            returnType = null;
            return false;
        }

        (func, returnType) = impl.Value;
        return true;
    }

    internal (Func<CommandInvocationArguments, object?> Shim, Type ReturnType)? GetInvokableInternal(ParserContext ctx)
    {
        if (!TryGetConcreteMethod(ctx.Bundle.PipedType, ctx.Bundle.TypeArguments, out var concreteMethod))
            return null;

        var method = concreteMethod.Value.Info;

        var args = Expression.Parameter(typeof(CommandInvocationArguments));
        var paramList = new List<Expression>();

        foreach (var param in method.GetParameters())
        {
            paramList.Add(GetParamExpr(param, ctx.Bundle.PipedType, args));
        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), method, paramList);

        var returnType = method.ReturnType;
        if (returnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));
        else if (returnType.IsValueType)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        var func = Expression.Lambda<Func<CommandInvocationArguments, object?>>(partialShim, args).Compile();
        return (func, returnType);
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

    /// <summary>
    /// Struct for caching information about a command's methods. Helps reduce LINQ & reflection calls when attempting
    /// to find matching methods.
    /// </summary>
    internal readonly struct CommandMethod
    {
        /// <summary>
        /// The method associated with some command.
        /// </summary>
        public readonly MethodInfo Info;

        /// <summary>
        /// The argument associated with the piped value.
        /// </summary>
        public readonly ParameterInfo? PipeArg;

        /// <summary>
        /// The number of expected type arguments for commands associated with generic methods.
        /// </summary>
        public readonly int NumTypeParams;

        public readonly bool IsGeneric;

        /// <summary>
        /// Whether the type of the piped value should be used as one of the type parameters for generic methods.
        /// I.e., whether the method has a <see cref="TakesPipedTypeAsGenericAttribute"/>.
        /// </summary>
        public readonly bool PipeGeneric;

        public CommandMethod(MethodInfo info)
        {
            Info = info;
            PipeArg = info.ConsoleGetPipedArgument();

            if (!info.IsGenericMethodDefinition)
            {
                NumTypeParams = 0;
                return;
            }

            IsGeneric = true;
            PipeGeneric = info.HasCustomAttribute<TakesPipedTypeAsGenericAttribute>();

            if (!PipeGeneric)
            {
                NumTypeParams = info.GetGenericArguments().Length;
                return;
            }

            NumTypeParams = info.GetGenericArguments().Length - 1;
        }
    }

    internal readonly record struct ConcreteCommandMethod(MethodInfo Info, CommandArgument[] Args);
    internal readonly record struct CommandArgument(string Name, Type Type, ITypeParser Parser);

    public override string ToString()
    {
        return FullName;
    }
}

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
