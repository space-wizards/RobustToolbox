using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.LanguageServer.Provider;

using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public sealed class DiagnosticProvider : IPostInjectInit
{
    [Dependency] private readonly DocumentCache _cache = null!;
    [Dependency] private readonly ELLanguageServer _server = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;

    public void PostInject()
    {
        _cache.DocumentChanged += OnDocumentChanged;
    }

    private void OnDocumentChanged(Uri uri)
    {
        Console.Error.WriteLine($"Diagnostics - Document changed! Uri: {uri}");

        List<Diagnostic> diagnosticList = new();

        var errors = _cache.GetErrors(uri);

        if (errors != null)
        {
            foreach (var (path, nodeList) in errors)
            {
                Console.Error.WriteLine($"Error in file: {path}");

                foreach (var node in nodeList)
                {
                    Console.Error.WriteLine(
                        $"* {node.Node} - {node.ErrorReason} - {node.AlwaysRelevant} - {node.Node.Start} -> {node.Node.End}");

                    diagnosticList.Add(new Diagnostic()
                        {
                            Message = node.ErrorReason,
                            Range = new DocumentRange()
                            {
                                Start = new Position()
                                {
                                    Line = node.Node.Start.Line - 1,
                                    Character = node.Node.Start.Column - 1
                                },
                                End = new Position()
                                {
                                    Line = node.Node.End.Line - 1,
                                    Character = node.Node.End.Column - 1
                                }
                            },
                            Severity = DiagnosticSeverity.Error,
                            Source = "SS14 LSP",
                            Code = "12313",
                        }
                    );
                }
            }
        }

        _server.Client.PublishDiagnostics(new PublishDiagnosticsParams()
        {
            Uri = uri,
            Diagnostics = diagnosticList
        });
    }
}
