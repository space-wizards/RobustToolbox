using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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

        error = new InvalidUsername(Loc, word);
        error.Contextualize(parser.Input, (start, parser.Index));
        return false;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        var opts = CompletionHelper.SessionNames(true, _player);
        return (CompletionResult.FromHintOptions(opts, "<player session>"), null);
    }

    public record InvalidUsername(ILocalizationManager Loc, string Username) : IConError
    {
        public FormattedMessage DescribeInner()
        {
            return FormattedMessage.FromMarkup(Loc.GetString("cmd-parse-failure-session", ("username", Username)));
        }

        public string? Expression { get; set; }
        public Vector2i? IssueSpan { get; set; }
        public StackTrace? Trace { get; set; }
    }
}
