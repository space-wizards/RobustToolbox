using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Robust.Shared.Collections;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer;

internal sealed class SemanticTokensHandler : ISemanticTokensFullHandler
{
    private readonly ILogger<SemanticTokensHandler> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TextDocumentsCache _cache;

    public SemanticTokensHandler(
        ILogger<SemanticTokensHandler> logger,
        ILoggerFactory loggerFactory,
        TextDocumentsCache cache)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _cache = cache;
    }

    public Task<SemanticTokens?> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        var tokens = new ValueList<SemanticTokenData>();

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

                if (map.TryGetNode("id", out var idNode) && idNode is YamlScalarNode idScalar &&
                    idScalar.Value != null)
                {
                    var start = Helpers.ToLsp(idScalar.Start);
                    var end = Helpers.ToLsp(idScalar.End);

                    ref var token = ref tokens.AddRef();
                    token.TokenType = 0;
                    token.Length = end.Character - start.Character;
                    token.Line = start.Line;
                    token.StartChar = start.Character;
                }

                if (map.TryGetNode("type", out var typeNode) && typeNode is YamlScalarNode typeScalar &&
                    typeScalar.Value != null)
                {
                    var start = Helpers.ToLsp(typeScalar.Start);
                    var end = Helpers.ToLsp(typeScalar.End);

                    ref var token = ref tokens.AddRef();
                    token.TokenType = 1;
                    token.Length = end.Character - start.Character;
                    token.Line = start.Line;
                    token.StartChar = start.Character;
                }
            }
        }

        tokens.Sort();

        var lastLine = 0;
        var lastChar = 0;

        var ints = new int[tokens.Count * 5];
        for (var i = 0; i < tokens.Count; i++)
        {
            ref var token = ref tokens[i];
            var ii = i * 5;
            var dl = token.Line - lastLine;
            ints[ii + 0] = dl; // Delta line
            ints[ii + 1] = token.StartChar; // Delta start char.
            if (dl == 0)
                ints[ii + 1] -= lastChar;

            ints[ii + 2] = token.Length; // Length
            ints[ii + 3] = token.TokenType; // Token type
            ints[ii + 4] = token.TokenModifiers; // Token modifier.

            lastLine = token.Line;
            lastChar = token.StartChar;
        }

        return Task.FromResult<SemanticTokens?>(new SemanticTokens
        {
            Data = ImmutableArray.Create(ints)
        });
    }

    public SemanticTokensRegistrationOptions GetRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        var options = new SemanticTokensRegistrationOptions
        {
            Full = true,
            Legend = new SemanticTokensLegend
            {
                TokenModifiers = Array.Empty<SemanticTokenModifier>(),
                TokenTypes = new[]
                {
                    SemanticTokenType.Class,
                    SemanticTokenType.Enum,
                }
            },
            Range = false
        };

        return options;
    }

    private struct SemanticTokenData : IComparable<SemanticTokenData>
    {
        public int Line;
        public int StartChar;
        public int Length;
        public int TokenType;
        public int TokenModifiers;

        public int CompareTo(SemanticTokenData other)
        {
            var cmp = Line.CompareTo(other.Line);
            if (cmp != 0)
                return cmp;

            return StartChar.CompareTo(other.StartChar);
        }

        public static bool operator <(SemanticTokenData left, SemanticTokenData right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(SemanticTokenData left, SemanticTokenData right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(SemanticTokenData left, SemanticTokenData right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(SemanticTokenData left, SemanticTokenData right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
