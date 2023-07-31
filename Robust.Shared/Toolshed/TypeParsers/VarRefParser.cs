using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class VarRefParser<T> : TypeParser<ValueRef<T>>
{
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        error = null;
        parser.Consume(char.IsWhiteSpace);

        var chkpoint = parser.Save();
        var success = _toolshed.TryParse<T>(parser, out var value, out error);

        if (error is UnparseableValueError)
            error = null;

        if (value is not null && success)
        {
            result = new ValueRef<T>((T)value);
            error = null;
            return true;
        }

        parser.Restore(chkpoint);

        if (parser.EatMatch('$'))
        {
            // We're parsing a variable.
            if (parser.GetWord(x => char.IsLetterOrDigit(x) || x == '_') is not { } word)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            result = new ValueRef<T>(word);
            error = null;
            return true;
        }
        else
        {
            if (Block<T>.TryParse(false, parser, null, out var block, out _, out error))
            {
                result = new ValueRef<T>(block);
                return true;
            }

            result = null;
            return false;
        }
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        parser.Consume(char.IsWhiteSpace);

        if (parser.EatMatch('$'))
        {
            return (CompletionResult.FromHint("<variable name>"), null);
        }
        else
        {
            var chkpoint = parser.Save();
            if (Block<T>.TryParse(false, parser, null, out _, out var result, out _))
            {
                if (result is not null)
                    return await result.Value;
            }
            parser.Restore(chkpoint);

            var (res, err) = await _toolshed.TryAutocomplete(parser, typeof(T), null);

            if (err is not UnparseableValueError || res is not null)
            {
                return (CompletionResult.FromHintOptions(res?.Options ?? Array.Empty<CompletionOption>(),$"<variable, block, or value of type {typeof(T)}>"), err);
            }

            return (CompletionResult.FromHint("$<variable name>"), null);
        }
    }
}
