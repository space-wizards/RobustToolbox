using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.Core;

namespace Robust.LanguageServer;

public static class Helpers
{
    // public static Range ToLsp(Mark start, Mark end)
    // {
    //     return new Range(ToLsp(start), ToLsp(end));
    // }

    public static Position ToLsp(Mark mark)
    {
        return new Position((int)(mark.Line - 1), (int)(mark.Column - 1));
    }

    public static Position ToLsp(NodeMark mark)
    {
        return new Position((int)(mark.Line - 1), (int)(mark.Column - 1));
    }
}
