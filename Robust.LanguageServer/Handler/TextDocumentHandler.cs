using System.Text;
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

        var content = _cache.GetDocumentContents(request.TextDocument.Uri);

        foreach (var change in request.ContentChanges)
        {
            if (change.Range is null && change.RangeLength is null)
            {
                // This is a full content update
                content = change.Text;
                continue;
            }

            if (change.Range is not {} range)
            {
                _logger.Error("Missing range for incremental change");
                continue;
            }

            // Incremental update
            _logger.Error($"Got incremental update: {range} => {change.Text}");

            var start = PosToIndex(range.Start, content);
            var end = PosToIndex(range.End, content);

            var writer = new StringBuilder();
            writer.Append(content.Substring(0, start));
            writer.Append(change.Text);
            writer.Append(content.Substring(end));
            content = writer.ToString();
        }

        _cache.UpdateDocument(request.TextDocument.Uri, request.TextDocument.Version, content);

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
            Change = TextDocumentSyncKind.Incremental,
            OpenClose = true,
            WillSave = true,
            WillSaveWaitUntil = true,
            Save = true
        };
    }

    private static int PosToIndex(Position pos, string content)
    {
        var index = 0;

        for (var i = 0; i < pos.Line; ++i)
        {
            index += content.Substring(index).IndexOf('\n') + 1;
        }

        index += pos.Character;

        return index;
    }
}
