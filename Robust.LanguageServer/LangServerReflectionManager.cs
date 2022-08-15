using Robust.Shared.Reflection;

namespace Robust.LanguageServer;

public sealed class LangServerReflectionManager : ReflectionManager
{
    protected override IEnumerable<string> TypePrefixes => Prefixes;

    private static readonly string[] Prefixes =
    {
        "",

        "Robust.Client.",
        "Content.Client.",

        "Robust.Shared.",
        "Content.Shared.",

        "Robust.Server.",
        "Content.Server.",
    };
}
