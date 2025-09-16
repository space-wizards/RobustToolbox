using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// Default parser for resource paths.
/// </summary>
/// <remarks>
/// This will generate completions suggestions using <see cref="CompletionHelper.UserFilePath"/>. If you want to
/// customize suggestions, you need to use a custom type parser. E.g., see <see cref="ContentPathParser"/>
/// </remarks>
public sealed class ResPathTypeParser : TypeParser<ResPath>
{
    [Dependency] private readonly IResourceManager _resMan = default!;

    /// <summary>
    /// Tokens that make up a valid unquoted path.
    /// Paths containing other symbols (e.g., whitespace or semicolons) need to be in quotes.
    /// </summary>
    public static bool IsPathToken(Rune c) => ParserContext.IsToken(c)
                                              || c == new Rune('/')
                                              || c == new Rune('-')
                                              || c == new Rune('.');

    public override bool TryParse(ParserContext ctx, out ResPath result)
    {
        if (!TryParse(ctx, out var nullableResult, partial: false))
        {
            result = default;
            return false;
        }

        result = nullableResult.Value;
        return true;
    }

    /// <summary>
    /// Attempt to parse a resource path.
    /// </summary>
    /// <param name="ctx">The parser context.</param>
    /// <param name="path">The parsed path.</param>
    /// <param name="partial">If true, this will succeed even if the path is quoted but is missing a closing quote.
    /// This is intended to be used by other path-parsers that want to generate completion options for partially
    /// complete paths.
    /// </param>
    public static bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ResPath? path, bool partial)
    {
        path = default;
        string? str;

        // Paths can be specified without quotes, though they may be necessary if the path contains spaces other special
        // characters (anything that is fails IsPathToken()).
        if (ctx.PeekRune() == new Rune('"'))
        {
            if (!StringTypeParser.TryParse(ctx, out str, partial))
                return false;
        }
        else
        {
            str = ctx.GetWord(IsPathToken);
        }

        if (str == null)
            return false;

        path = new(str);
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = GetArgHint(arg);

        if (!TryParse(ctx, out var p, partial: true) || p is not {} path)
            return CompletionResult.FromHint(hint);


        var opts = CompletionHelper.UserFilePath(
            path.CanonPath,
            _resMan.UserData,
            flags: CompletionOptionFlags.AlwaysQuote);

        return CompletionResult.FromHintOptions(opts, hint);
    }
}

/// <summary>
/// Parent class for custom ResPath parsers that just want to modify the completion suggestions.
/// </summary>
public abstract class CustomPathParser : CustomTypeParser<ResPath>
{
    [Dependency] protected readonly IResourceManager ResMan = default!;

    protected abstract IEnumerable<CompletionOption> GetCompletions(ResPath path);

    public override bool TryParse(ParserContext ctx, out ResPath result)
    {
        return Toolshed.TryParse(ctx, out result);
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = GetArgHint(arg);
        if (!ResPathTypeParser.TryParse(ctx, out var path, partial: true))
            return CompletionResult.FromHint(hint);

        return CompletionResult.FromHintOptions(GetCompletions(path.Value), hint);
    }
}

/// <summary>
/// Custom <see cref="ResPath"/> parser that uses <see cref="CompletionHelper.ContentFilePath"/> to generate completions.
/// </summary>
public sealed class ContentPathParser : CustomPathParser
{
    protected override IEnumerable<CompletionOption> GetCompletions(ResPath path)
        => CompletionHelper.ContentFilePath(path.CanonPath, ResMan, flags: CompletionOptionFlags.AlwaysQuote);
}

/// <summary>
/// Custom <see cref="ResPath"/> parser that uses <see cref="CompletionHelper.ContentDirPath"/> to generate completions.
/// </summary>
public sealed class ContentDirPathParser : CustomPathParser
{
    protected override IEnumerable<CompletionOption> GetCompletions(ResPath path)
        => CompletionHelper.ContentDirPath(path.CanonPath, ResMan, flags: CompletionOptionFlags.AlwaysQuote);
}

/// <summary>
/// Custom <see cref="ResPath"/> parser that uses <see cref="CompletionHelper.AudioFilePath"/> to generate completions.
/// </summary>
public sealed class AudioPathParser : CustomPathParser
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override IEnumerable<CompletionOption> GetCompletions(ResPath path)
        => CompletionHelper.AudioFilePath(path.CanonPath, _proto, ResMan, flags: CompletionOptionFlags.AlwaysQuote);
}

/// <summary>
/// Custom <see cref="ResPath"/> parser that does not generate any auto-completion options.
/// </summary>
public sealed class GenericPathParser : CustomTypeParser<ResPath>
{
    public override bool TryParse(ParserContext ctx, out ResPath result)
    {
        return Toolshed.TryParse(ctx, out result);
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}
