using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Definition;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.LanguageServer.Handler;

public sealed class DefinitionHandler : DefinitionHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;
    [Dependency] private readonly LanguageServerContext _context = null!;

    private readonly ISawmill _logger = Logger.GetSawmill("DefinitionHandler");

    protected override Task<DefinitionResponse?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        _logger.Error($"DefinitionHandler.Handle - {_context.RootDirectory?.LocalPath}");

        DefinitionResponse? result = null;

        // NOTE: Copy-paste from HoverHandler here

        var fields = _cache.GetFields(request.TextDocument.Uri);
        if (fields != null && _context.RootDirectory != null)
        {
            if (GetFieldAtPosition(fields, request.Position) is { } field)
            {
                if (field.FieldInfo.DeclaringType is { } type && type.Namespace is {} ns)
                {
                    _logger.Error($"Field {field.FieldInfo.Name} declared in type {type.Name} in {type.Namespace}");

                    var parts = ns.Split(".");

                    // Namespace is assumed to start with a Foo.Bar assembly name
                    if (parts.Length > 2)
                    {
                        var assembly = parts[0] + "." + parts[1];
                        _logger.Error($"Assembly {assembly}");

                        UriBuilder uriBuilder = new UriBuilder(_context.RootDirectory);
                        uriBuilder.Path += Path.DirectorySeparatorChar + assembly;
                        uriBuilder.Path += Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, parts.Skip(2));
                        uriBuilder.Path += Path.DirectorySeparatorChar + $"{type.Name}.cs";
                        _logger.Info($"UriBuilder: {uriBuilder.Uri.LocalPath}");

                        var path = assembly + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, parts.Skip(2)) + Path.DirectorySeparatorChar + $"{type.Name}.cs";

                        // var fullPath = new Uri(_context.RootDirectory, path);
                        var fullPath = uriBuilder.Uri;
                        _logger.Error($"Path {_context.RootDirectory} + {path}");
                        _logger.Error($"Path {fullPath}");
                        foreach (var part in parts.Skip(2))
                        {
                            _logger.Error($"Part: [{part}]");
                        }

                        _logger.Error($"Path {fullPath}");
                        _logger.Error($"Path {fullPath.LocalPath}");
                        _logger.Error($"Path {fullPath.AbsolutePath}");
                        if (File.Exists(fullPath.LocalPath))
                        {
                            result = new DefinitionResponse(new Location(uriBuilder.Uri,
                                new DocumentRange()
                                {
                                    Start = new Position(0, 0),
                                    End = new Position(0, 1)
                                }
                            ));
                        }
                    }
                }
            }
        }

        return Task.FromResult(result)!;
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.DefinitionProvider = true;
    }

    // NOTE: More copy-paste, move to shared code if used
    private static FieldDefinition? GetFieldAtPosition(
        List<(ValueDataNode, FieldDefinition)> fields,
        Position position)
    {
        foreach (var (node, field) in fields)
        {
            if (node.Start.Line - 1 == position.Line &&
                position.Character >= node.Start.Column - 1
                && position.Character <= node.End.Column - 1)
            {
                return field;
            }
        }

        return null;
    }
}
