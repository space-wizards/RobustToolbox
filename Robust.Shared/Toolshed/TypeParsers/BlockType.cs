using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// This custom type parser is used for parsing the type returned by a <see cref="Block{T}"/> command argument.
/// </summary>
public sealed class BlockOutputParser : CustomTypeParser<Type>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        result = null;
        var save = ctx.Save();
        var start = ctx.Index;
        if (!Block.TryParseBlock(ctx, null, null, out var block))
        {
            ctx.Error?.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }
        ctx.Restore(save);

        if (block.ReturnType == null)
            return false;

        result = block.ReturnType;
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, string? argName)
    {
        Block.TryParseBlock(ctx, null, null, out _);
        return ctx.Completions;
    }
}

// TODO TOOLSHED Improve Block-type parsers
// See the comment in the remark.. Ideally the type parser should be able to know this.
// But currently type parsers are only used once per command, not once per method/implementation.
/// <summary>
/// This custom type parser is used for parsing the type returned by a <see cref="Block{T,T}"/>, where the block's input
/// type is inferred from the type being piped into the command that is currently being parsed.
/// </summary>
/// <remarks>
/// If the piped type is an <see cref="IEnumerable{T}"/>, it is assumed that the blocks input type is the enumerable
/// generic argument. I.e., we assume that the command has an implementation where the parameter with the
/// <see cref="PipedArgumentAttribute"/> is also an <see cref="IEnumerable{T}"/>
/// </remarks>>
public sealed class MapBlockOutputParser : CustomTypeParser<Type>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        result = null;

        var pipeType = ctx.Bundle.PipedType;
        if (pipeType != null && pipeType.IsGenericType(typeof(IEnumerable<>)))
            pipeType = pipeType.GetGenericArguments()[0];

        var save = ctx.Save();
        var start = ctx.Index;
        if (!Block.TryParseBlock(ctx, pipeType, null, out var block))
        {
            ctx.Error?.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        ctx.Restore(save);

        if (block.ReturnType == null)
            return false;

        result = block.ReturnType;
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, string? argName)
    {
        var pipeType = ctx.Bundle.PipedType;
        if (pipeType != null && pipeType.IsGenericType(typeof(IEnumerable<>)))
            pipeType = pipeType.GetGenericArguments()[0];

        Block.TryParseBlock(ctx, pipeType, null, out _);
        return ctx.Completions;
    }
}
