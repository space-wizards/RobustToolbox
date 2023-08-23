using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public abstract class SpanLikeTypeParser<T, TElem> : TypeParser<T>
    where T : notnull
    where TElem : unmanaged
{
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public abstract int Elements { get; }
    public abstract T Create(Span<TElem> elements);

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        if (!parserContext.EatMatch('['))
        {
            error = new ExpectedOpenBrace();
            result = null;
            return false;
        }
        parserContext.ConsumeWhitespace();

        parserContext.PushTerminator("]");

        Span<TElem> elements = stackalloc TElem[Elements];

        for (var i = 0; i < Elements; i++)
        {
            var checkpoint = parserContext.Save();
            if (!_toolshed.TryParse<TElem>(parserContext, out var value, out error))
            {
                parserContext.Restore(checkpoint);

                var start = parserContext.Index;
                if (parserContext.EatTerminator())
                {
                    error = new UnexpectedCloseBrace();
                    error.Contextualize(parserContext.Input, new Vector2i(start, parserContext.Index));
                }

                result = null;
                return false;
            }

            parserContext.ConsumeWhitespace();

            if (i + 1 < Elements && parserContext.EatTerminator())
            {
                error = new UnexpectedCloseBrace();
                result = null;
                return false;
            }

            if (i + 1 < Elements && !parserContext.TryMatch(","))
            {
                error = new ExpectedComma();
                result = null;
                return false;
            }

            elements[i] = value;
            parserContext.ConsumeWhitespace();
        }



        if (!parserContext.EatTerminator())
        {
            error = new ExpectedCloseBrace();
            result = null;
            return false;
        }

        error = null;
        result = Create(elements);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint(typeof(T).PrettyName()), null));
    }
}


public record UnexpectedCloseBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup("Unexpected closing brace.");
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
