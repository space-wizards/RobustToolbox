using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentColor;
using EmmyLua.LanguageServer.Framework.Protocol.Message.SemanticToken;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer.Handler;

public sealed class DocumentColorHandler : DocumentColorHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;

    // private DocumentColorBuilder Builder { get; } = new();

    protected override Task<DocumentColorResponse> Handle(DocumentColorParams request, CancellationToken token)
    {
        Console.Error.WriteLine("DocumentColorHandler");
        // var uri = request.TextDocument.Uri.UnescapeUri;
        var text = _cache.GetDocumentContents(request.TextDocument.Uri);


        List<ColorInformation> colors = new();


        using var sr = new StringReader(_cache.GetDocumentContents(request.TextDocument.Uri));

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

                if (map.TryGetNode("color", out var colorNode) && colorNode is YamlScalarNode colorScalar &&
                    colorScalar.Value != null)
                {
                    var start = Helpers.ToLsp(colorScalar.Start);
                    var end = Helpers.ToLsp(colorScalar.End);

                    if (Color.TryParse(colorScalar.Value, out var color))
                    {
                        colors.Add(new()
                        {
                            Color = new(color.R, color.G, color.B, color.A),
                            Range = new(start, end)
                        });
                    }

                    // ref var token = ref tokens.AddRef();
                    // token.TokenType = 0;
                    // token.Length = end.Character - start.Character;
                    // token.Line = start.Line;
                    // token.StartChar = start.Character;

                    // semanticTokenBuilder.Push(start,
                    //     end.Character - start.Character,
                    //     SemanticTokenTypes.Class);
                }
            }
        }

        //
        // context.ReadyRead(() =>
        // {
        //     var semanticModel = context.GetSemanticModel(uri);
        //     if (semanticModel is not null)
        //     {
        //         container = new DocumentColorResponse(Builder.Build(semanticModel));
        //     }
        // });

        // DocumentColorResponse container = new();

        var container = new DocumentColorResponse(colors);

        return Task.FromResult(container)!;
    }

    protected override Task<ColorPresentationResponse> Resolve(ColorPresentationParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"DocumentColorHandler Resolve {request.TextDocument.Uri} - {request.Color}");
        var uri = request.TextDocument.Uri.UnescapeUri;

        ColorPresentationResponse container = null!;

        // var color = new Color((float)request.Color.Red, (float)request.Color.Green, (float)request.Color.Blue);

        if (request.Range.HasValue)
        {
            var r = (int)(request.Color.Red * 255);
            var g = (int)(request.Color.Green * 255);
            var b = (int)(request.Color.Blue * 255);

            var newText = $"#{r:X2}{g:X2}{b:X2}";

            container = new ColorPresentationResponse([
                new()
                {
                    Label = newText,
                    TextEdit = new ()
                    {
                        Range = request.Range.Value,
                        NewText = $"\"{newText}\""
                    }
                }
            ]);
        }

        // context.ReadyRead(() =>
        // {
        //     var semanticModel = context.GetSemanticModel(uri);
        //     if (semanticModel is not null)
        //     {
        //         container = new ColorPresentationResponse(Builder.ModifyColor(request, semanticModel));
        //     }
        // });

        return Task.FromResult(container);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.ColorProvider = true;
    }
}
