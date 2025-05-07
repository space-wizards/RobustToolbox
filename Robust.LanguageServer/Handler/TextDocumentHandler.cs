using Robust.Shared.Log;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.TextDocument;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

namespace Robust.LanguageServer.Handler;

public class TextDocumentHandler : TextDocumentHandlerBase
{
    [Dependency] private readonly ELLanguageServer _server = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;
    [Dependency] private readonly DocumentCache _cache = null!;

    private ISawmill _logger;

    public TextDocumentHandler()
    {
        _logger = Logger.GetSawmill("TextDocumentHandler");
    }

    protected override Task Handle(DidOpenTextDocumentParams request, CancellationToken token)
    {
        _logger.Info($"DidOpenTextDocument {request.TextDocument.Uri}");
        _cache.UpdateDocument(request.TextDocument.Uri, request.TextDocument.Version, request.TextDocument.Text);
        return Task.CompletedTask;
    }

    protected override Task Handle(DidChangeTextDocumentParams request, CancellationToken token)
    {
        _logger.Info($"DidChangeTextDocument {request.TextDocument.Uri}");

        if (request.ContentChanges.Count != 1)
            throw new NotImplementedException();

        var change = request.ContentChanges[0];
        if (change.Range is not null || change.RangeLength is not null)
            throw new NotImplementedException();

        _cache.UpdateDocument(request.TextDocument.Uri, request.TextDocument.Version, change.Text);

        // var text = change.Text;
        return Task.CompletedTask;
    }

    protected override Task Handle(DidCloseTextDocumentParams request, CancellationToken token)
    {
        _logger.Info($"DidCloseTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task Handle(WillSaveTextDocumentParams request, CancellationToken token)
    {
        _logger.Info($"WillSaveTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task<List<TextEdit>?> HandleRequest(WillSaveTextDocumentParams request, CancellationToken token)
    {
        _logger.Info($"WillSaveTextDocumentRequest {request.TextDocument.Uri}");
        return Task.FromResult<List<TextEdit>?>(null);
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            Change = TextDocumentSyncKind.Full,
            OpenClose = true,
            WillSave = true,
            WillSaveWaitUntil = true,
            Save = true
        };
    }
}
