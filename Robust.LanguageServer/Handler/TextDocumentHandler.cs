namespace Robust.LanguageServer.Handler;

using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.TextDocument;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public class TextDocumentHandler : TextDocumentHandlerBase
{
    private readonly ELLanguageServer _server;

    [Dependency] public readonly IPrototypeManager _protoMan = null!;

    public TextDocumentHandler(ELLanguageServer server)
    {
        _server = server;
        // _protoMan = IoCManager.Resolve<IPrototypeManager>();
    }

    protected override Task Handle(DidOpenTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: DidOpenTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task Handle(DidChangeTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: DidChangeTextDocument {request.TextDocument.Uri}");

        if (request.ContentChanges.Count != 1)
            throw new NotImplementedException();

        var change = request.ContentChanges[0];
        if (change.Range is not null || change.RangeLength is not null)
            throw new NotImplementedException();

        var text = change.Text;
        // var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        // var reader = new StreamReader(stream);
        var reader = new StringReader(text);
        List<Diagnostic> diagnosticList = new();

        try
        {
            var errors = _protoMan.ValidateSingleFile(reader, out var protos, request.TextDocument.Uri.ToString());

            Console.Error.WriteLine($"Errors: {errors.Count} Protos: {protos.Count}");

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
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error: {e}");

            diagnosticList.Add(new Diagnostic()
                {
                    Message = e.Message,
                    Range = new DocumentRange()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 0
                        },
                        End = new Position()
                        {
                            Line = 99,
                            Character = 0
                        }
                    },
                    Severity = DiagnosticSeverity.Error,
                    Source = "test",
                    Code = "12313",
                }
            );
        }

        // _protoMan.ValidateSingleFile()

        _server.Client.PublishDiagnostics(new PublishDiagnosticsParams()
        {
            Uri = request.TextDocument.Uri,
            Diagnostics = diagnosticList
        });
        return Task.CompletedTask;
    }

    protected override Task Handle(DidCloseTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: DidCloseTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task Handle(WillSaveTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: WillSaveTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task<List<TextEdit>?> HandleRequest(WillSaveTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: WillSaveTextDocumentRequest {request.TextDocument.Uri}");
        return Task.FromResult<List<TextEdit>?>(null);
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            Change = TextDocumentSyncKind.Full,
            OpenClose = true,
            WillSave = true,
            WillSaveWaitUntil = true,
            Save = true
        };
    }
}
