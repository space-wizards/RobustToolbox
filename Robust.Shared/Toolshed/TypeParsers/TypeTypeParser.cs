using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class TypeTypeParser : TypeParser<Type>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        throw new NotImplementedException("C# level types are not yet supported in parsing.");

        var res = parser.GetWord();
        // TODO: Do this right.
        result = Type.GetType(res ?? "", false, true);
        /* TODO
        if (!_modLoader.IsContentTypeAccessAllowed((Type?) result ?? typeof(void)))
        {
            error = new TypeIsSandboxViolation((Type?) result ?? typeof(void));
            result = null;
            return false;
        }
        */
        error = null;
        return result is not null;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint("C# level type"), null));
    }
}

internal record struct TypeIsSandboxViolation(Type T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The type {T.PrettyName()} is not permitted under sandbox rules.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
