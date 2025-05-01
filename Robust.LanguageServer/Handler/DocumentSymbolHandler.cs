using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentSymbol;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.LanguageServer.Handler;

public sealed class DocumentSymbolHandler : DocumentSymbolHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;

    private readonly ISawmill _logger = Logger.GetSawmill("DocumentSymbolHandler");

    protected override Task<DocumentSymbolResponse> Handle(DocumentSymbolParams request, CancellationToken token)
    {
        var symbols = _cache.GetSymbols(request.TextDocument.Uri);

        DocumentSymbolResponse? result = null;

        if (symbols != null)
        {
            List<DocumentSymbol> documentSymbols = new();

            foreach (var symbol in symbols)
            {
                documentSymbols.Add(new()
                {
                    Name = symbol.Name,
                    Kind = SymbolKind.Class,
                    Range = new()
                    {
                        Start = Helpers.ToLsp(symbol.NodeStart),
                        End = Helpers.ToLsp(symbol.NodeEnd)
                    },
                    SelectionRange = new()
                    {
                        Start = Helpers.ToLsp(symbol.NodeStart),
                        End = Helpers.ToLsp(symbol.NodeEnd)
                    }
                });
            }

            result = new DocumentSymbolResponse(documentSymbols);
        }

        _logger.Error("DocumentSymbol");

        return Task.FromResult(result);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.DocumentSymbolProvider = true;
    }
}
