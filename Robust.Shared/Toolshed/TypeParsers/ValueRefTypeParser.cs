using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ValueRefTypeParser<T> : TypeParser<ValueRef<T>>
{
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = _toolshed.TryParse<ValueRef<T, T>>(parser, out var inner, out error);
        result = null;
        if (res)
            result = new ValueRef<T>((ValueRef<T, T>)inner!);
        return res;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser, string? argName)
    {
        return _toolshed.TryAutocomplete(parser, typeof(ValueRef<T, T>), argName);
    }
}

internal sealed class VarRefParser<T, TAuto> : TypeParser<ValueRef<T, TAuto>>
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
            result = new ValueRef<T, TAuto>((T)value);
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

            result = new ValueRef<T, TAuto>(word);
            error = null;
            return true;
        }
        else
        {
            if (Block<T>.TryParse(false, parser, null, out var block, out _, out error))
            {
                result = new ValueRef<T, TAuto>(block);
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
            var (res, err) = await _toolshed.TryAutocomplete(parser, typeof(TAuto), null);
            parser.Restore(chkpoint);
            CompletionOption[] parseOptions = Array.Empty<CompletionOption>();
            if (err is not UnparseableValueError || res is not null)
            {
                parseOptions = res?.Options ?? parseOptions;
            }

            chkpoint = parser.Save();
            Block<T>.TryParse(true, parser, null, out _, out var result, out _);
            if (result is not null)
            {
                var (blockRes, _) = await result.Value;
                var options = blockRes?.Options ?? Array.Empty<CompletionOption>();
                return (CompletionResult.FromHintOptions(parseOptions.Concat(options).ToArray(), $"<variable, block, or value of type {typeof(T).PrettyName()}>"), err);
            }
            parser.Restore(chkpoint);

            return (CompletionResult.FromHint("$<variable name>"), null);
        }
    }
}
