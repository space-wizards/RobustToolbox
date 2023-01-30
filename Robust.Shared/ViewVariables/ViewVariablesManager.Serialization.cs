using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    private static string[] ParseArguments(string arguments)
    {
        var args = new List<string>();
        var parentheses = false;
        var builder = new StringBuilder();
        var i = 0;

        while (i < arguments.Length)
        {
            var current = arguments[i];
            switch (current)
            {
                case '(':
                    parentheses = true;
                    break;
                case ')' when parentheses:
                    parentheses = false;
                    break;
                case ',' when !parentheses:
                    args.Add(builder.ToString());
                    builder.Clear();
                    break;
                default:
                    if (!parentheses && char.IsWhiteSpace(current))
                        break;
                    builder.Append(current);
                    break;
            }
            i++;
        }

        if(builder.Length != 0)
            args.Add(builder.ToString());

        return args.ToArray();
    }

    private bool TryDeserializeArguments(
        Type[] argumentTypes,
        int optionalArguments,
        string[] arguments,
        [NotNullWhen(true)] out object?[]? args,
        out string? error)
    {
        if (arguments.Length < argumentTypes.Length - optionalArguments || arguments.Length > argumentTypes.Length)
        {
            error = $"Incorrect number of arguments. Expected between {argumentTypes.Length - optionalArguments} and {argumentTypes.Length} but got {argumentTypes.Length}";
            args = null;
            return false;
        }

        var parameters = new List<object?>();

        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var type = argumentTypes[i];

            if (!TryDeserializeValue(type, argument, out var value, out error))
            {
                args = null;
                return false;
            }

            parameters.Add(value);
        }

        for (var i = 0; i < argumentTypes.Length - arguments.Length; i++)
        {
            parameters.Add(Type.Missing);
        }

        args = parameters.ToArray();
        error = null;
        return true;
    }

    private bool TryDeserializeValue(Type type, string value, out object? result, out string? error)
    {
        error = null;
        // Check if the argument is a VV path, and if not, deserialize the value with serv3.
        if (TryResolvePath(value, out var vvObj, out _) && vvObj.Get() is {} resolved && resolved.GetType().IsAssignableTo(type))
        {
            result = resolved;
            return true;
        }

        try
        {
            // Here we go serialization moment
            using TextReader stream = new StringReader(value);
            var yamlStream = new YamlStream();
            yamlStream.Load(stream);
            var document = yamlStream.Documents[0];
            var rootNode = document.RootNode;
            result = _serMan.Read(type, rootNode.ToDataNode(), _context);
            return true;
        }
        catch (Exception e)
        {
            error = e.ToString();
            result = null;
            return false;
        }
    }

    private string? SerializeValue(Type type, object? value, string? nodeTag = null)
    {
        if (value == null || type == typeof(void))
            return null;

        var node = _serMan.WriteValue(type, value, true, _context);

        // Don't replace an existing tag if it's null.
        if(!string.IsNullOrEmpty(nodeTag))
            node.Tag = nodeTag;

        var document = new YamlDocument(node.ToYamlNode());
        var stream = new YamlStream {document};

        using var writer = new StringWriter(new StringBuilder());

        // Remove the three funny dots from the end of the string...
        stream.Save(new YamlNoDocEndDotsFix(new YamlMappingFix(new Emitter(writer))), false);
        return writer.ToString();
    }
}
