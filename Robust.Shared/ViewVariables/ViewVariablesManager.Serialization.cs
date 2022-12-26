using System;
using System.Collections.Generic;
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

    private object?[]? DeserializeArguments(Type[] argumentTypes, int optionalArguments, string[] arguments)
    {
        // Incorrect number of arguments!
        if (arguments.Length < argumentTypes.Length - optionalArguments || arguments.Length > argumentTypes.Length)
            return null;

        var parameters = new List<object?>();

        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var type = argumentTypes[i];

            var value =  DeserializeValue(type, argument);

            parameters.Add(value);
        }

        for (var i = 0; i < argumentTypes.Length - arguments.Length; i++)
        {
            parameters.Add(Type.Missing);
        }

        return parameters.ToArray();
    }

    private object? DeserializeValue(Type type, string value)
    {
        // Check if the argument is a VV path, and if not, deserialize the value with serv3.
        if (ResolvePath(value)?.Get() is {} resolved && resolved.GetType().IsAssignableTo(type))
            return resolved;

        try
        {
            // Here we go serialization moment
            using TextReader stream = new StringReader(value);
            var yamlStream = new YamlStream();
            yamlStream.Load(stream);
            var document = yamlStream.Documents[0];
            var rootNode = document.RootNode;
            return _serMan.Read(type, rootNode.ToDataNode());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string? SerializeValue(Type type, object? value, string? nodeTag = null)
    {
        if (value == null || type == typeof(void))
            return null;

        var node = _serMan.WriteValue(type, value, true);

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
