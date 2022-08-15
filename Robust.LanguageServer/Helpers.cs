using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using YamlDotNet.Core;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Robust.LanguageServer;

public static class Helpers
{
    public static Range ToLsp(Mark start, Mark end)
    {
        return (ToLsp(start), ToLsp(end));
    }

    public static Position ToLsp(Mark mark)
    {
        return (mark.Line - 1, mark.Column - 1);
    }
}
