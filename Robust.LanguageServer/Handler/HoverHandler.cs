using System.Text;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.LanguageServer.Handler;

public sealed class HoverHandler : HoverHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;
    [Dependency] private readonly DocsManager _docs = null!;

    private readonly ISawmill _logger = Logger.GetSawmill("HoverHandler");

    protected override Task<HoverResponse?> Handle(HoverParams request, CancellationToken token)
    {
        _logger.Error($"HoverHandler.Handle: - {request.Position}");

        HoverResponse? response = null;

        var fields = _cache.GetFields(request.TextDocument.Uri);
        if (fields != null)
        {
            if (GetFieldAtPosition(fields, request.Position) is { } field)
            {
                var commentsObj = _docs.GetComments(field.FieldInfo.MemberInfo);
                string comments = commentsObj.Summary?.Trim() ?? string.Empty;
                string remarks = commentsObj.Remarks?.Trim() ?? string.Empty;
                _logger.Error($"comments: {commentsObj} [{comments}] - {remarks}");

                response = new HoverResponse()
                {
                    Contents = new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = $"""
                            ```c#
                            {field.FieldInfo.DeclaringType?.Name}.{field.FieldInfo.Name} ({FormatType(field.FieldType)})
                            ```
                            ___
                            {comments}
                            {remarks}
                            """
                    }
                };
            }
        }

        return Task.FromResult(response);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.HoverProvider = true;
    }

    private string FormatType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return $"{FormatType(underlyingType)}?";

        if (type.IsGenericType)
        {
            var genTypeDef = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();

            // "List`2" => "List" (Surely there’s a proper way to do this?)
            var genericTypeName =
                genTypeDef.Name.Substring(0, genTypeDef.Name.LastIndexOf("`", StringComparison.InvariantCulture));

            StringBuilder builder = new StringBuilder();
            foreach (var arg in arguments)
            {
                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(FormatType(arg));
            }

            return genericTypeName + "<" + builder + ">";
        }

        if (type == typeof(Single))
            return "float";
        // if (type == typeof(Double))
        //     return "double";
        // if (type == typeof(Decimal))
        //     return "decimal";
        if (type == typeof(Boolean))
            return "bool";
        // if (type == typeof(Int32))
        //     return "int";

        return type.Name;
    }

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
