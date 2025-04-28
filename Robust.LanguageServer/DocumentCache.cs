using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Robust.LanguageServer.Parsing;
using Robust.Shared.IoC;

namespace Robust.LanguageServer;

public sealed class DocumentCache
{
    [Dependency] private readonly Parser _parser = null!;

    // This should really store the parsed document
    // but for now we’ll just hold the string contents
    private Dictionary<Uri, string> _documents = new();

    public delegate void DocumentChangedHandler(Uri uri);

    public event DocumentChangedHandler? DocumentChanged;

    // event DocumentParsed
    // event DocumentParseFailed

    public string GetDocumentContents(DocumentUri uri)
    {
        return _documents[uri.Uri];
    }

    public void UpdateDocument(DocumentUri uri, string content)
    {
        _documents[uri.Uri] = content;
        DocumentChanged?.Invoke(uri.Uri);

        _parser.ParseDocument(content);
    }
}
