using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ComponentTypeParser : TypeParser<ComponentType>
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parserContext.Index;
        var word = parserContext.GetWord(ParserContext.IsToken);
        error = null;

        if (word is null)
        {
            error = new OutOfInputError();
            result = null;
            return false;
        }

        if (!_factory.TryGetRegistration(word.ToLower(), out var reg, true))
        {
            result = null;
            error = new UnknownComponentError(word);
            error.Contextualize(parserContext.Input, (start, parserContext.Index));
            return false;
        }

        result = new ComponentType(reg.Type);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>(
            (CompletionResult.FromOptions(_factory.AllRegisteredTypes.Select(_factory.GetComponentName)), null)
            );
    }
}

public readonly record struct ComponentType(Type Ty) : IAsType<Type>
{
    public Type AsType() => Ty;
};

public record struct UnknownComponentError(string Component) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup(
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
