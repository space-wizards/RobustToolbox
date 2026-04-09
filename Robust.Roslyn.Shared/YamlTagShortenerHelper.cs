namespace Robust.Roslyn.Shared;

public static class YamlTagShortenerHelper
{
    public static string ReplaceLast(string currentString, string stringToReplace, string replacement)
    {
        var lastStart = currentString.LastIndexOf(stringToReplace, StringComparison.Ordinal);
        return currentString.Remove(lastStart, stringToReplace.Length) + replacement;
    }
}
