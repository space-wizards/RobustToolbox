using System.Text;

namespace Robust.Shared.Toolshed.Syntax;

public sealed partial class ParserContext
{
    public bool NoMultilineExprs = false;

    public static bool IsToken(Rune c) => Rune.IsLetterOrDigit(c) || c == new Rune('_');

    public static bool IsCommandToken(Rune c)
    {
        if (Rune.IsLetterOrDigit(c))
            return true;

        if (Rune.IsWhiteSpace(c))
            return false;

        return c != new Rune('{')
               && c != new Rune('}')
               && c != new Rune('[')
               && c != new Rune(']')
               && c != new Rune('(')
               && c != new Rune(')')
               && c != new Rune('"')
               && c != new Rune('\'')
               && c != new Rune(':')
               && c != new Rune(';')
               && c != new Rune('|')
               && c != new Rune('$')
               && !Rune.IsControl(c);
    }

    public static bool IsNumeric(Rune c)
        =>
            IsToken(c)
            || c == new Rune('+')
            || c == new Rune('-')
            || c == new Rune('.')
            || c == new Rune('%');
}
