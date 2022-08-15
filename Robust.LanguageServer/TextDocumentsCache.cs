using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Robust.LanguageServer;

public sealed class TextDocumentsCache : TextDocumentSyncHandlerBase
{
    private readonly Dictionary<DocumentUri, CachedTextDocument> _documents = new();

    public CachedTextDocument Get(DocumentUri uri) => _documents[uri];

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "yaml");
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var doc = new CachedTextDocument
        {
            Text = request.TextDocument.Text
        };

        _documents.Add(request.TextDocument.Uri, doc);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var doc = _documents[request.TextDocument.Uri];

        doc.Version = request.TextDocument.Version;

        foreach (var change in request.ContentChanges)
        {
            if (change.Range != null)
                throw new NotSupportedException("Incremental updates not currently supported!");

            doc.Text = change.Text;
        }

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _documents.Remove(request.TextDocument.Uri);

        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        SynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions(TextDocumentSyncKind.Full);
    }
}

public sealed class CachedTextDocument
{
    public string Text { get; set; } = "";
    public int? Version { get; set; }
}
