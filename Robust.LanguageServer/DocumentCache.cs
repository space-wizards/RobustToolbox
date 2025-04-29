using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Robust.LanguageServer.Parsing;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer;

internal sealed class DocumentCache
{
    [Dependency] private readonly Parser _parser = null!;
    [Dependency] private readonly IPrototypeManagerInternal _protoMan = null!;

    // This should really store the parsed document
    // but for now we’ll just hold the string contents
    private Dictionary<Uri, string> _documents = new();

    private Dictionary<Uri, Dictionary<string, HashSet<ErrorNode>>> _errors = new();
    private Dictionary<Uri, List<(ValueDataNode, FieldDefinition)>> _fields = new();
    private Dictionary<Uri, List<DocumentSymbol>> _symbols = new();

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
        var errors = _protoMan.AnalyzeSingleFile(reader,
            out var symbols,
            out var fields,
            uri.Uri.ToString());

        _symbols[uri.Uri] = symbols;
        _fields[uri.Uri] = fields;
        _errors[uri.Uri] = errors;
    }

    public Dictionary<string, HashSet<ErrorNode>>? GetErrors(DocumentUri uri)
    {
        return _errors.GetValueOrDefault(uri.Uri);
    }

    public List<(ValueDataNode, FieldDefinition)>? GetFields(DocumentUri uri)
    {
        return _fields.GetValueOrDefault(uri.Uri);
    }

    public List<DocumentSymbol>? GetSymbols(DocumentUri uri)
    {
        return _symbols.GetValueOrDefault(uri.Uri);
    }
}
