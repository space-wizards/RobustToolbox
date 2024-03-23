using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// Parse a username to an <see cref="ICommonSession"/>
/// </summary>
internal sealed class SessionTypeParser : TypeParser<ICommonSession>
{
    [Dependency] private ISharedPlayerManager _player = default!;

    public override bool TryParse(ParserContext parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parser.Index;
        var word = parser.GetWord();
        error = null;
        result = null;

        if (word == null)
        {
            error = new OutOfInputError();
            return false;
        }

        if (_player.TryGetSessionByUsername(word, out var session))
        {
            result = session;
            return true;
        }

        error = new InvalidUsername(Loc.GetString("cmd-parse-failure-session", ("username", word)));
        error.Contextualize(parser.Input, (start, parser.Index));
        return false;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        var opts = CompletionHelper.SessionNames(true, _player);
        return (CompletionResult.FromHintOptions(opts, "<Session>"), null);
    }

    public record InvalidUsername(string msg) : IConError
    {
        public FormattedMessage DescribeInner()
        {
            return FormattedMessage.FromMarkup(msg);
        }

        public string? Expression { get; set; }
        public Vector2i? IssueSpan { get; set; }
        public StackTrace? Trace { get; set; }
    }
}
