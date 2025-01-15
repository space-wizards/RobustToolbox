using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class ComponentTypeParser : CustomTypeParser<Type>
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        result = null;
        var start = ctx.Index;
        var word = ctx.GetWord(ParserContext.IsToken);

        if (word is null)
        {
            ctx.Error = new OutOfInputError();
            return false;
        }

        if (!_factory.TryGetRegistration(word.ToLower(), out var reg, true))
        {
            ctx.Error = new UnknownComponentError(word);
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        result = reg.Type;
        return true;
    }

    public override CompletionResult TryAutocomplete(
        ParserContext parserContext,
        CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(_factory.AllRegisteredTypes.Select(_factory.GetComponentName), GetArgHint(arg));
    }
}

public record struct UnknownComponentError(string Component) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromUnformatted(
            $"Unknown component {Component}. For a list of all components, try types:components."
            );
        if (Component.EndsWith("component", true, CultureInfo.InvariantCulture))
        {
            msg.PushNewline();
            msg.AddText($"Do not specify the word `Component` in the argument. Maybe try {Component[..^"component".Length]}?");
        }

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
