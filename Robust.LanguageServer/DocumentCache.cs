using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Robust.LanguageServer.Parsing;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.LanguageServer;

public sealed class DocumentCache
{
    [Dependency] private readonly Parser _parser = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;

    // This should really store the parsed document
    // but for now we’ll just hold the string contents
    private Dictionary<Uri, string> _documents = new();

    private Dictionary<Uri, Dictionary<string, HashSet<ErrorNode>>> _errors = new();
    private Dictionary<Uri, List<(ValueDataNode, object)>> _fields = new();

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

        using var reader = new StringReader(content);
        var errors = _protoMan.ValidateSingleFile(reader,
            out var _,
            out var fields,
            uri.Uri.ToString());

        _fields[uri.Uri] = fields;
        _errors[uri.Uri] = errors;
    }

    public Dictionary<string, HashSet<ErrorNode>>? GetErrors(DocumentUri uri)
    {
        return _errors.GetValueOrDefault(uri.Uri);
    }

    public List<(ValueDataNode, object)>? GetFields(DocumentUri uri)
    {
        return _fields.GetValueOrDefault(uri.Uri);
    }
}
