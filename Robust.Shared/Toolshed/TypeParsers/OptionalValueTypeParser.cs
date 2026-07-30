using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class OptionalValueTypeParser<T> : TypeParser<OptionalValue<T>>
{
    public override bool TryParse(ParserContext ctx, out OptionalValue<T> result)
    {
        result = default;
        if (!Toolshed.TryParse(ctx, typeof(T), out var parsed)) return false;
        if (parsed is not T value)
            return false;
        result = new OptionalValue<T>(value, true);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var result = Toolshed.TryAutocomplete(ctx, typeof(T), arg);
        return new CompletionResult(result?.Options ?? [], GetArgHint(arg));
    }
}

public readonly record struct OptionalValue<T>(T Value, bool Assigned);
