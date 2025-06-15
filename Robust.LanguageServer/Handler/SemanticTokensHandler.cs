using System.Collections.Immutable;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Common;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.SemanticToken;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer.Handler;

public sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;

    private readonly ISawmill _logger = Logger.GetSawmill("HoverHandler");

    private List<string> TokenTypes { get; init; } =
    [
        SemanticTokenTypes.Class,
        SemanticTokenTypes.Enum
    ];

    private List<string> TokenModifiers { get; init; } =
    [
        // SemanticTokenModifiers.Documentation
    ];

    protected override Task<SemanticTokens?> Handle(
        SemanticTokensParams semanticTokensParams,
        CancellationToken cancellationToken)
    {
        _logger.Info($"Handling semantic tokens for {semanticTokensParams}");

        using var sr = new StringReader(_cache.GetDocumentContents(semanticTokensParams.TextDocument.Uri));

        var yaml = new YamlStream();
        yaml.Load(sr);

        var semanticTokenBuilder = new SemanticTokensBuilder(TokenTypes, TokenModifiers);

        foreach (var document in yaml.Documents)
        {
            var root = document.RootNode;
            if (root is not YamlSequenceNode seq)
                continue;

            foreach (var child in seq.Children)
            {
                if (child is not YamlMappingNode map)
                    continue;

                if (map.TryGetNode("id", out var idNode) && idNode is YamlScalarNode idScalar &&
                    idScalar.Value != null)
                {
                    var start = Helpers.ToLsp(idScalar.Start);
                    var end = Helpers.ToLsp(idScalar.End);

                    semanticTokenBuilder.Push(start, end.Character - start.Character, SemanticTokenTypes.Class);
                }

                if (map.TryGetNode("type", out var typeNode) && typeNode is YamlScalarNode typeScalar &&
                    typeScalar.Value != null)
                {
                    var start = Helpers.ToLsp(typeScalar.Start);
                    var end = Helpers.ToLsp(typeScalar.End);

                    semanticTokenBuilder.Push(start, end.Character - start.Character, SemanticTokenTypes.Enum);
                }
            }
        }

        return Task.FromResult(new SemanticTokens()
        {
            Data = semanticTokenBuilder.Build()
        })!;
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.SemanticTokensProvider = new SemanticTokensOptions()
        {
            Legend = new SemanticTokensLegend()
            {
                TokenTypes = TokenTypes,
                TokenModifiers = TokenModifiers,
            },
            Full = true,
            Range = false, // XXX
        };
    }


    protected override Task<SemanticTokensDeltaResponse?> Handle(
        SemanticTokensDeltaParams semanticTokensDeltaParams,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task<SemanticTokens?> Handle(
        SemanticTokensRangeParams semanticTokensRangeParams,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
