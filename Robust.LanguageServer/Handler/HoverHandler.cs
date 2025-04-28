using System.Text;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.LanguageServer.Handler;

public sealed class HoverHandler : HoverHandlerBase
{
    [Dependency] private readonly DocumentCache _cache = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;
    [Dependency] private readonly DocsManager _docs = null!;

    protected override Task<HoverResponse?> Handle(HoverParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"HoverHandler.Handle: - {request.Position}");

        HoverResponse? response = null;

        var fields = _cache.GetFields(request.TextDocument.Uri);
        if (fields != null)
        {
            foreach (var (node, fieldObj) in fields)
            {
                if (node.Start.Line - 1 == request.Position.Line &&
                    request.Position.Character - 1 >= node.Start.Column
                    && request.Position.Character - 1 <= node.End.Column)
                {
                    if (fieldObj is FieldDefinition field)
                    {
                        var commentsObj = _docs.GetComments(field.FieldInfo.MemberInfo);
                        string comments = commentsObj.Summary?.Trim() ?? string.Empty;
                        Console.Error.WriteLine($"comments: {commentsObj} [{comments}]");

                        response = new HoverResponse()
                        {
                            Contents = new MarkupContent()
                            {
                                Kind = MarkupKind.Markdown,
                                Value = $"""
                                    ```c#
                                    public {FormatType(field.FieldType)} {field.FieldInfo.Name};
                                    ```
                                    ___

                                    {comments}
                                    """
                            }
                        };
                    }

                    break;
                }
            }
        }

/*
        try
        {
            var errors = _protoMan.ValidateSingleFile(reader,
                out var protos,
                out var fields,
                request.TextDocument.Uri.ToString());

            foreach (var (node, fieldObj) in fields)
            {
                if (node.Start.Line-1 == request.Position.Line &&
                    request.Position.Character-1 >= node.Start.Column
                    && request.Position.Character-1 <= node.End.Column)
                {
                    if (fieldObj is FieldDefinition field)
                    {
                        response = new HoverResponse()
                        {
                            Contents = new MarkupContent()
                            {
                                Kind = MarkupKind.Markdown,
                                Value = $"""
                                ```c#
                                public {FormatType(field.FieldType)} {field.FieldInfo.Name};
                                ```
                                ___
                                {field.FieldInfo.DeclaringType?.Name}
                                {field.FieldInfo.MemberInfo}
                                """
                            }
                        };
                    }

                    break;
                }
            }
        }
        catch (Exception e)
        {
            // ignored
        }
*/

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
            var genericTypeName = genTypeDef.Name.Substring(0, genTypeDef.Name.LastIndexOf("`", StringComparison.InvariantCulture));

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
}
