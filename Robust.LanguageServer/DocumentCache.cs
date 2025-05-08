using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Robust.LanguageServer.Parsing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer;

internal sealed class DocumentCache : IPostInjectInit
{
    [Dependency] private readonly IPrototypeManagerInternal _protoMan = null!;

    private ISawmill _logger = default!;

    // This should really store the parsed document
    // but for now we’ll just hold the string contents
    private readonly Dictionary<Uri, string> _documents = new();

    private readonly Dictionary<Uri, HashSet<ErrorNode>> _errors = new();
    private readonly Dictionary<Uri, List<(ValueDataNode, FieldDefinition)>> _fields = new();
    private readonly Dictionary<Uri, List<DocumentSymbol>> _symbols = new();

    public delegate void DocumentChangedHandler(Uri uri, int documentVersion);

    public event DocumentChangedHandler? DocumentChanged;

    public string GetDocumentContents(DocumentUri uri)
    {
        return _documents[uri.Uri];
    }

    public void UpdateDocument(DocumentUri uri, int version, string content)
    {
        _documents[uri.Uri] = content;

        try
        {
            _logger.Info($"Updating document {uri.Uri}");

            using var reader = new StringReader(content);
            var errors = _protoMan.AnalyzeSingleFile(reader,
                out var symbols,
                out var fields,
                uri.Uri.ToString());

            _symbols[uri.Uri] = symbols;
            _fields[uri.Uri] = fields;
            _errors[uri.Uri] = errors;
        }
        catch (Exception e)
        {
            _logger.Error($"Caught exception parsing document: {e}");
            _symbols.Remove(uri.Uri);
            _fields.Remove(uri.Uri);
            _errors.Remove(uri.Uri);

            // Add a mock error node which wraps the exception message.
            // These error nodes might need to be converted to an internal type
            // but ideally any exceptions will be migrated to actual errors that we can report
            // line:col info for, so it may not be necessary.
            _errors[uri.Uri] = [new ErrorNode(new ValueDataNode(), $"Parsing failed: {e.Message}")];
        }

        DocumentChanged?.Invoke(uri.Uri, version);
    }

    public HashSet<ErrorNode>? GetErrors(DocumentUri uri)
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

    public void PostInject()
    {
        _logger = Logger.GetSawmill("DocumentCache");
    }
}
