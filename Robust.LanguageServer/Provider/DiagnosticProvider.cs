using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;

namespace Robust.LanguageServer.Provider;

using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public sealed class DiagnosticProvider : IPostInjectInit
{
    [Dependency] private readonly DocumentCache _cache = null!;
    [Dependency] private readonly ELLanguageServer _server = null!;

    private ISawmill _logger = null!;
    public void PostInject()
    {
        _cache.DocumentChanged += OnDocumentChanged;
        _logger = Logger.GetSawmill("DiagnosticProvider");
    }

    private void OnDocumentChanged(Uri uri, int documentVersion)
    {
        _logger.Error($"Document changed! Uri: {uri}");

        List<Diagnostic> diagnosticList = new();

        if (_cache.GetErrors(uri) is {} errors)
        {
            foreach (var errorNode in errors)
            {
                _logger.Error($"Error in file: {uri}");

                _logger.Error(
                    $"* {errorNode.Node} - {errorNode.ErrorReason} - {errorNode.AlwaysRelevant} - {errorNode.Node.Start} -> {errorNode.Node.End}");


                diagnosticList.Add(new Diagnostic()
                {
                    Message = errorNode.ErrorReason,
                    Range = Helpers.LspRangeForNode(errorNode.Node),
                    Severity = DiagnosticSeverity.Error,
                    Source = "SS14 LSP",
                    Code = "12313",
                });
            }
        }

        _server.Client.PublishDiagnostics(new PublishDiagnosticsParams()
        {
            Uri = uri,
            Diagnostics = diagnosticList,
            Version = documentVersion,
        });
    }
}
