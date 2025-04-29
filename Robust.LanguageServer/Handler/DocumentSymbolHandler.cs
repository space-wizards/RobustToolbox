using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentSymbol;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;

namespace Robust.LanguageServer.Handler;

public sealed class DocumentSymbolHandler : DocumentSymbolHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;

    protected override Task<DocumentSymbolResponse> Handle(DocumentSymbolParams request, CancellationToken token)
    {
        var symbols = _cache.GetSymbols(request.TextDocument.Uri);

        DocumentSymbolResponse? result = null;

        if (symbols != null)
        {
            List<DocumentSymbol> documentSymbols = new();

            foreach (var (name, type, node) in symbols)
            {
                documentSymbols.Add(new()
                {
                    Name = name,
                    Kind = SymbolKind.Class,
                    Range = new()
                    {
                        Start = Helpers.ToLsp(node.Start),
                        End = Helpers.ToLsp(node.End)
                    },
                    SelectionRange = new()
                    {
                        Start = Helpers.ToLsp(node.Start),
                        End = Helpers.ToLsp(node.End)
                    }
                });
            }

            result = new DocumentSymbolResponse(documentSymbols);
        }
        //
        // new DocumentSymbolResponse([
        //     new DocumentSymbol()
        //     {
        //         Name = "DocumentSymbol",
        //         Kind = SymbolKind.Class,
        //         Range = new()
        //         {
        //             Start = new Position(0, 0),
        //             End = new Position(0, 1)
        //         },
        //         SelectionRange = new()
        //         {
        //             Start = new Position(0, 0),
        //             End = new Position(0, 1)
        //         }
        //     }
        // ]);

        Console.Error.WriteLine("DocumentSymbol");
        return Task.FromResult(result);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.DocumentSymbolProvider = true;
    }
}
