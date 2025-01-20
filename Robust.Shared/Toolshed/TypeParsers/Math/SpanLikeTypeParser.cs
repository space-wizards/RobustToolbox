using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public abstract class SpanLikeTypeParser<T, TElem> : TypeParser<T>
    where T : notnull
    where TElem : unmanaged
{
    public abstract int Elements { get; }
    public abstract T Create(Span<TElem> elements);

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T? result)
    {
        if (!ctx.EatMatch('['))
        {
            ctx.Error = new ExpectedOpenBrace();
            result = default;
            return false;
        }
        ctx.ConsumeWhitespace();

        ctx.PushBlockTerminator(']');

        Span<TElem> elements = stackalloc TElem[Elements];

        for (var i = 0; i < Elements; i++)
        {
            var checkpoint = ctx.Save();
            if (!Toolshed.TryParse<TElem>(ctx, out var value))
            {
                ctx.Restore(checkpoint);

                var start = ctx.Index;
                if (ctx.EatBlockTerminator())
                {
                    ctx.Error = new UnexpectedCloseBrace();
                    ctx.Error.Contextualize(ctx.Input, new Vector2i(start, ctx.Index));
                }

                result = default;
                return false;
            }

            ctx.ConsumeWhitespace();

            if (i + 1 < Elements && ctx.EatBlockTerminator())
            {
                ctx.Error = new UnexpectedCloseBrace();
                result = default;
                return false;
            }

            if (i + 1 < Elements && !ctx.EatMatch(','))
            {
                ctx.Error = new ExpectedComma();
                result = default;
                return false;
            }

            elements[i] = value;
            ctx.ConsumeWhitespace();
        }

        if (!ctx.EatBlockTerminator())
        {
            ctx.Error = new ExpectedCloseBrace();
            result = default;
            return false;
        }

        result = Create(elements);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHint(typeof(T).PrettyName());
    }
}


public record UnexpectedCloseBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Unexpected closing brace.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record ExpectedComma : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a comma in the sequence.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}


public record ExpectedOpenBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected an opening brace, [");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record ExpectedCloseBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a closing brace, ]");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
