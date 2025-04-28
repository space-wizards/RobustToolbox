using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using EmmyLua.LanguageServer.Framework.Server.Handler;

namespace Robust.LanguageServer.Handler;

public sealed class HoverHandler : HoverHandlerBase
{

    protected override Task<HoverResponse?> Handle(HoverParams request, CancellationToken token)
    {
        Console.Error.WriteLine("HoverHandler.Handle");
        return Task.FromResult(new HoverResponse()
        {
            Contents = new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = "Hello World"
            }
        })!;
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities, ClientCapabilities clientCapabilities)
    {
        serverCapabilities.HoverProvider = true;
    }
}
