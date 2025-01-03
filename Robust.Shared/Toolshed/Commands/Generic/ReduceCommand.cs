using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class ReduceCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Reduce<T>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        [CommandArgument(typeof(ReduceBlockParser))] Block reducer
    )
    {
        var localCtx = new LocalVarInvocationContext(ctx);
        localCtx.SetLocal("value", default(T));

        using var enumerator = input.GetEnumerator();

        if (!enumerator.MoveNext())
            throw new InvalidOperationException($"Input contains no elements");

        var result = enumerator.Current;

        while (enumerator.MoveNext())
        {
            localCtx.SetLocal("value", enumerator.Current);
            result = (T) reducer.Invoke(result, localCtx)!;
            if (ctx.HasErrors)
                break;
        }

        return result;
    }

    /// <summary>
    /// Custom block parser for the <see cref="ReduceCommand"/> that is aware of the "$value" variable.
    /// </summary>
    private sealed class ReduceBlockParser : CustomTypeParser<Block>
    {
        public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block? result)
        {
            result = null;
            if (ctx.Bundle.PipedType is not {IsGenericType: true})
                return false;

            var localParser = new LocalVarParser(ctx.VariableParser);
            var type = ctx.Bundle.PipedType.GetGenericArguments()[0];
            localParser.SetLocalType("value", type, false);
            ctx.VariableParser = localParser;

            if (!Block.TryParseBlock(ctx, type, type, out var run))
            {
                result = null;
                ctx.VariableParser = localParser.Inner;
                return false;
            }

            ctx.VariableParser = localParser.Inner;
            result = new Block(run);
            return true;
        }

        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            if (ctx.Bundle.PipedType is not {IsGenericType: true})
                return null;

            var localParser = new LocalVarParser(ctx.VariableParser);
            var type = ctx.Bundle.PipedType.GetGenericArguments()[0];
            localParser.SetLocalType("value", type, false);
            ctx.VariableParser = localParser;
            Block.TryParseBlock(ctx, type, type, out _);
            ctx.VariableParser = localParser.Inner;
            return ctx.Completions;
        }
    }
}
