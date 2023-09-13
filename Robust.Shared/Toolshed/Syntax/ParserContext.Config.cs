using System.Text;

namespace Robust.Shared.Toolshed.Syntax;

public sealed partial class ParserContext
{
    public bool NoMultilineExprs = false;

    public static bool IsToken(Rune c)
        => (Rune.IsLetter(c) || Rune.IsDigit(c) || c == new Rune('_')) && !Rune.IsWhiteSpace(c);

    public static bool IsCommandToken(Rune c)
        =>
            c != new Rune('{')
        && c != new Rune('}')
        && c != new Rune('[')
        && c != new Rune(']')
        && c != new Rune('(')
        && c != new Rune(')')
        && c != new Rune('"')
        && c != new Rune('\'')
        && c != new Rune(':')
        && !Rune.IsWhiteSpace(c)
        && !Rune.IsControl(c);

    public static bool IsNumeric(Rune c)
        =>
            IsToken(c)
            || c == new Rune('+')
            || c == new Rune('-')
            || c == new Rune('.')
            || c == new Rune('%');

    public static bool IsTerminator(Rune c)
        => (Rune.IsSymbol(c) || Rune.IsPunctuation(c) || Rune.IsSeparator(c) || c == new Rune('}')) && !Rune.IsWhiteSpace(c);
}
