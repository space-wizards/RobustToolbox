using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class VarRefParser<T> : TypeParser<VarRef<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        parser.Consume(char.IsWhiteSpace);

        if (parser.EatMatch('$'))
        {
            // We're parsing a variable.
            if (parser.GetWord(char.IsLetterOrDigit) is not { } word)
            {
                result = null;
                error = null;
                return false;
            }

            result = new VarRef<T>(word);
            error = null;
            return true;
        }
        else
        {
            if (Block<T>.TryParse(false, parser, null, out var block, out _, out error))
            {
                result = new VarRef<T>(block);
                error = null;
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
            if (Block<T>.TryParse(false, parser, null, out var block, out var result, out _))
            {
                if (result is not null)
                    return await result.Value;
            }

            return (CompletionResult.FromHint("$<variable name>"), null);
        }
    }
}
