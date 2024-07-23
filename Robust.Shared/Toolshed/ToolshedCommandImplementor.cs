using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

internal sealed class ToolshedCommandImplementor
{
    [Dependency] private readonly ToolshedManager _toolshedManager = default!;
    public required ToolshedCommand Owner;

    public required string? SubCommand;

    public Dictionary<CommandDiscriminator, Func<CommandInvocationArguments, object?>> Implementations = new();

    public ToolshedCommandImplementor()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    ///     You who tread upon this dreaded land, what is it that brings you here?
    ///     For this place is not for you, this land of terror and death.
    ///     It brings fear to all who tread within, terror to the homes ahead.
    ///     Begone, foul maintainer, for this place is not for thee.
    /// </summary>
    public bool TryParseArguments(
            bool doAutocomplete,
            ParserContext parserContext,
            string? subCommand,
            Type? pipedType,
            [NotNullWhen(true)] out Dictionary<string, object?>? args,
            out Type[] resolvedTypeArguments,
            out IConError? error,
            out ValueTask<(CompletionResult?, IConError?)>? autocomplete
        )
    {
        resolvedTypeArguments = new Type[Owner.TypeParameterParsers.Length];

        var firstStart = parserContext.Index;

        // HACK: This is for commands like Map until I have a better solution.
        if (Owner.GetType().GetCustomAttribute<MapLikeCommandAttribute>() is {} mapLike)
        {
            var start = parserContext.Index;
            // We do our own parsing, assuming this is some kind of map-like operation.
            var chkpoint = parserContext.Save();
            if (!Block.TryParse(doAutocomplete, parserContext, mapLike.TakesPipedType ? pipedType!.GetGenericArguments()[0] : null, out var block, out autocomplete, out error))
            {
                error?.Contextualize(parserContext.Input, (start, parserContext.Index));
                resolvedTypeArguments = Array.Empty<Type>();
                args = null;
                return false;
            }

            resolvedTypeArguments[0] = block.CommandRun.Commands.Last().Item1.ReturnType!;
            parserContext.Restore(chkpoint);
            goto mapLikeDone;
        }

        for (var i = 0; i < Owner.TypeParameterParsers.Length; i++)
        {
            var start = parserContext.Index;
            var chkpoint = parserContext.Save();
            if (!_toolshedManager.TryParse(parserContext, Owner.TypeParameterParsers[i], out var parsed, out error) || parsed is not { } ty)
            {
                error?.Contextualize(parserContext.Input, (start, parserContext.Index));
                resolvedTypeArguments = Array.Empty<Type>();
                args = null;
                autocomplete = null;
                if (doAutocomplete)
                {
                    parserContext.Restore(chkpoint);
                    autocomplete = _toolshedManager.TryAutocomplete(parserContext, Owner.TypeParameterParsers[i], null);
                }

                return false;
            }

            Type real;
            if (ty is IAsType<Type> asTy)
            {
                real = asTy.AsType();
            }
            else if (ty is Type realTy)
            {
                real = realTy;
            }
            else
            {
                throw new NotImplementedException();
            }

            resolvedTypeArguments[i] = real;
        }

        mapLikeDone:
        var impls = Owner.GetConcreteImplementations(pipedType, resolvedTypeArguments, subCommand);
        if (impls.FirstOrDefault() is not { } impl)
        {
            args = null;
            error = new NoImplementationError(Owner.Name, resolvedTypeArguments, subCommand, pipedType, parserContext.Environment);
            error.Contextualize(parserContext.Input, (firstStart, parserContext.Index));
            autocomplete = null;
            return false;
        }

        autocomplete = null;
        args = new();
        foreach (var argument in impl.ConsoleGetArguments())
        {
            var start = parserContext.Index;
            var chkpoint = parserContext.Save();
            if (!_toolshedManager.TryParse(parserContext, argument.ParameterType, out var parsed, out error))
            {
                error?.Contextualize(parserContext.Input, (start, parserContext.Index));
                args = null;

                // Only generate auto-completions if the parsing error happened for the last argument.
                if (doAutocomplete && parserContext.Index > parserContext.MaxIndex)
                {
                    parserContext.Restore(chkpoint);
                    autocomplete = _toolshedManager.TryAutocomplete(parserContext, argument.ParameterType, null);
                }
                return false;
            }
            args[argument.Name!] = parsed;

            if (!doAutocomplete || parserContext.Index <= parserContext.MaxIndex)
                continue;

            // This was the end of the input, so we want to get completions for the current argument, not the next argument.
            doAutocomplete = false;
            var chkpoint2 = parserContext.Save();
            parserContext.Restore(chkpoint);
            autocomplete = _toolshedManager.TryAutocomplete(parserContext, argument.ParameterType, null);
            parserContext.Restore(chkpoint2);
        }

        error = null;
        return true;
    }

    /// <summary>
    ///     Attempts to generate a callable shim for a command, aka it's implementation, using the given types.
    /// </summary>
    public bool TryGetImplementation(Type? pipedType, Type[] typeArguments, [NotNullWhen(true)] out Func<CommandInvocationArguments, object?>? impl)
    {
        var discrim = new CommandDiscriminator(pipedType, typeArguments);

        if (Implementations.TryGetValue(discrim, out impl))
            return true;

        if (!Owner.TryGetReturnType(SubCommand, pipedType, typeArguments, out var ty))
        {
            impl = null;
            return false;
        }

        // Okay we need to build a new shim.

        var possibleImpls = Owner.GetConcreteImplementations(pipedType, typeArguments, SubCommand);

        IEnumerable<MethodInfo> impls;

        if (pipedType is null)
        {
            impls = possibleImpls.Where(x =>
                x.ConsoleGetPipedArgument() is {} param && param.ParameterType.CanBeEmpty()
                || x.ConsoleGetPipedArgument() is null
                || x.GetParameters().Length == 0);
        }
        else
        {
            impls = possibleImpls.Where(x =>
                x.ConsoleGetPipedArgument() is {} param && _toolshedManager.IsTransformableTo(pipedType, param.ParameterType)
                || x.IsGenericMethodDefinition);
        }

        var implArray = impls.ToArray();
        if (implArray.Length == 0)
        {
            return false;
        }

        var unshimmed = implArray.First();

        var args = Expression.Parameter(typeof(CommandInvocationArguments));

        var paramList = new List<Expression>();

        foreach (var param in unshimmed.GetParameters())
        {
            if (param.GetCustomAttribute<PipedArgumentAttribute>() is { } _)
            {
                if (pipedType is null)
                {
                    paramList.Add(param.ParameterType.CreateEmptyExpr());
                }
                else
                {
                    // (ParameterType)(args.PipedArgument)
                    paramList.Add(_toolshedManager.GetTransformer(pipedType, param.ParameterType, Expression.Field(args, nameof(CommandInvocationArguments.PipedArgument))));
                }

                continue;
            }

            if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
            {
                // (ParameterType)(args.Arguments[param.Name])
                paramList.Add(Expression.Convert(
                    Expression.MakeIndex(
                        Expression.Property(args, nameof(CommandInvocationArguments.Arguments)),
                        typeof(Dictionary<string, object?>).FindIndexerProperty(),
                        new [] {Expression.Constant(param.Name)}),
                    param.ParameterType));
                continue;
            }

            if (param.GetCustomAttribute<CommandInvertedAttribute>() is { } _)
            {
                // args.Inverted
                paramList.Add(Expression.Property(args, nameof(CommandInvocationArguments.Inverted)));
                continue;
            }

            if (param.GetCustomAttribute<CommandInvocationContextAttribute>() is { } _)
            {
                // args.Context
                paramList.Add(Expression.Property(args, nameof(CommandInvocationArguments.Context)));
                continue;
            }

        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), unshimmed, paramList);

        if (unshimmed.ReturnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));
        else if (ty is not null && ty.IsValueType)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        var lambda = Expression.Lambda<Func<CommandInvocationArguments, object?>>(partialShim, args);

        Implementations[discrim] = lambda.Compile();
        impl = Implementations[discrim];
        return true;
    }
}
