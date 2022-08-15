using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer;


internal sealed class DocumentSymbolHandler : IDocumentSymbolHandler
{
    private readonly ILogger<DocumentSymbolHandler> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TextDocumentsCache _cache;

    public DocumentSymbolHandler(
        ILogger<DocumentSymbolHandler> logger,
        ILoggerFactory loggerFactory,
        TextDocumentsCache cache)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _cache = cache;
    }

    public Task<SymbolInformationOrDocumentSymbolContainer> Handle(
        DocumentSymbolParams request,
        CancellationToken cancellationToken)
    {
        var list = new List<SymbolInformationOrDocumentSymbol>();

        using var sr = new StringReader(_cache.Get(request.TextDocument.Uri).Text);

        var yaml = new YamlStream();
        yaml.Load(sr);

        foreach (var document in yaml.Documents)
        {
            var root = document.RootNode;
            if (root is not YamlSequenceNode seq)
                continue;

            foreach (var child in seq.Children)
            {
                if (child is not YamlMappingNode map)
                    continue;

                if (map.TryGetNode("id", out var idNode) && idNode is YamlScalarNode idScalar && idScalar.Value != null)
                {
                    list.Add(new DocumentSymbol
                    {
                        Range = Helpers.ToLsp(idScalar.Start, idScalar.End),
                        Kind = SymbolKind.Class,
                        Name = idScalar.Value,
                        SelectionRange = Helpers.ToLsp(idScalar.Start, idScalar.End)
                    });
                }
            }
        }

        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer>(list);
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(
        DocumentSymbolCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentSymbolRegistrationOptions
        {
            DocumentSelector = new DocumentSelector(new DocumentFilter { Language = "yaml" })
        };
    }
}
