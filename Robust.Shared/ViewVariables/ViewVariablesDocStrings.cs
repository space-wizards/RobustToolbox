using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

/// <summary>
///     What kind of member a DocString key refers to.
/// </summary>
public enum ViewVariablesDocStringKind : byte
{
    /// <summary>
    ///     A type definition, looked up as <c>T:{key}</c>.
    /// </summary>
    Type,

    /// <summary>
    ///     A field or a property, looked up as <c>F:{key}</c> then <c>P:{key}</c>.
    /// </summary>
    FieldOrProperty,
}

/// <summary>
///     Loads and holds the trimmed xml docs that VV shows next to types and members.
/// </summary>
/// <remarks>
///     Docs for client and shared code live in <see cref="SharedRoot"/>, which ships to everyone.
///     Docs for server code live in <see cref="ServerRoot"/>, which only the server can see.
///     Clients get server DocStrings on demand over the network, see <c>MsgViewVariablesReqDocStrings</c>.
/// </remarks>
internal sealed class ViewVariablesDocStrings(ISawmill sawmill, params ResPath[] roots)
{
    public static readonly ResPath SharedRoot = new("/ViewVariables/");
    public static readonly ResPath ServerRoot = new("/Server/ViewVariables/");

    private readonly Dictionary<string, string> _docStrings = new();
    
    private bool _loaded;

    public void EnsureLoaded(IResourceManager resManager)
    {
        if (_loaded)
            return;

        foreach (var root in roots)
        {
            foreach (var resPath in resManager.ContentFindFiles(root))
            {
                if (resPath.Extension != "xml")
                    continue;

                LoadFile(resManager, resPath);
            }
        }

        _loaded = true;
    }

    public bool TryGet(ViewVariablesDocStringKind kind, string key, [NotNullWhen(true)] out string? docString)
    {
        if (!_loaded)
        {
            sawmill.Warning($"DocStrings haven't been loaded! Make sure {nameof(EnsureLoaded)} is called before any call to {TryGet}!");
            docString = null;
            return false;
        }

        switch (kind)
        {
            case ViewVariablesDocStringKind.Type:
                return _docStrings.TryGetValue($"T:{key}", out docString);
            case ViewVariablesDocStringKind.FieldOrProperty:
                // Will lumping fields and properties into the same search come back to bite us? Yes!
                // It's a problem for future someone to take care of. You'll have to make the server
                // send over a bool that toggles if we should look for a field or a property.
                return _docStrings.TryGetValue($"F:{key}", out docString)
                       || _docStrings.TryGetValue($"P:{key}", out docString);
            default:
                docString = null;
                return false;
        }
    }

    /// <summary>
    ///     Throws away anything loaded so far, the next lookup reloads from disk.
    /// </summary>
    public void Clear()
    {
        _docStrings.Clear();
        _loaded = false;
    }

    private void LoadFile(IResourceManager resManager, ResPath resPath)
    {
        using var resStream = resManager.ContentFileRead(resPath);

        var xmlDoc = new XmlDocument();
        try
        {
            // TODO: If the doc files ever become unreasonably large
            // then this should be switched to a stream.
            xmlDoc.Load(resStream);
        }
        catch (XmlException ex)
        {
            sawmill.Warning($"DocString xml file at `{resPath}` failed to load with exception: {ex}");
            return;
        }

        if (!TryGetChildNode(xmlDoc, "doc", out var docNode))
        {
            sawmill.Warning($"DocString xml file at `{resPath}` lacks `doc` node!");
            return;
        }

        if (!TryGetChildNode(docNode, "members", out var membersNode))
        {
            sawmill.Warning($"DocString xml file at `{resPath}` lacks `members` node!");
            return;
        }

        foreach (XmlNode memberNode in membersNode.ChildNodes)
        {
            // skips comment blocks, whitespace between elements
            if (memberNode.NodeType != XmlNodeType.Element)
                continue;

            DebugTools.Assert(memberNode.Name == "member");

            if (!TryGetAttributeText(memberNode, "name", out var memberName))
                continue;

            // skip method definitions
            if (memberName.StartsWith("M:"))
                continue;

            string docString;

            if (TryGetChildNode(memberNode, "viewvariables", out var viewVariablesNode))
            {
                docString = TrimLines(ProcessDocNode(viewVariablesNode));
            }
            else if (TryGetChildNode(memberNode, "summary", out var summaryNode))
            {
                docString = TrimLines(ProcessDocNode(summaryNode));
            }
            else
            {
                docString = "DocString is invalid.";
            }

            if (!_docStrings.TryAdd(memberName, docString))
            {
                sawmill.Warning($"DocString duplicate memberName `{memberName}` with docString `{docString}`");
            }
        }
    }

    private static string ProcessDocNode(XmlNode xmlNode)
    {
        var sb = new StringBuilder();
        foreach (XmlNode childNode in xmlNode.ChildNodes)
        {
            switch (childNode.NodeType)
            {
                case XmlNodeType.Text:
                    sb.Append(childNode.InnerText);
                    break;
                case XmlNodeType.Element:
                    ProcessXmlElement(childNode, sb);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Try to process the most common tags inside xml comment blocks.
    /// </summary>
    /// <param name="elementNode">The node to process.</param>
    /// <param name="sb">A <see cref="StringBuilder"/> that the result will be appended to.</param>
    /// <remarks>
    /// Ideally all the tags on <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags">this page</see>
    /// should be minimally supported.
    /// </remarks>
    private static void ProcessXmlElement(XmlNode elementNode, StringBuilder sb)
    {
        DebugTools.Assert(elementNode.NodeType == XmlNodeType.Element);
        switch (elementNode.Name)
        {
            case "see":
            {
                if (!string.IsNullOrEmpty(elementNode.InnerText))
                {
                    sb.Append(elementNode.InnerText);
                }
                else if (TryGetAttributeText(elementNode, "cref", out var crefText))
                {
                    sb.Append(crefText);
                }
                else if (TryGetAttributeText(elementNode, "langword", out var langwordText))
                {
                    sb.Append(langwordText);
                }
                break;
            }
            case "seealso":
            {
                DebugTools.Assert(string.IsNullOrEmpty(elementNode.InnerText));
                if (TryGetAttributeText(elementNode, "cref", out var crefText))
                {
                    sb.Append(crefText);
                }
                break;
            }
            case "b":     // bold text
            case "i":     // italic text

            case "c":     // text should be formatted as code
            case "code":  // text should be formatted as /multiline/ code
            case "para":  // paragraph text
            case "value": // value
            {
                DebugTools.Assert(!string.IsNullOrEmpty(elementNode.InnerText));
                sb.Append(elementNode.InnerText);
                break;
            }
            case "paramref":
            {
                DebugTools.Assert(string.IsNullOrEmpty(elementNode.InnerText));
                if (TryGetAttributeText(elementNode, "name", out var innerText))
                {
                    sb.Append(innerText);
                }
                break;
            }
            case "u":  // underline
            case "a":  // hyperlink
            case "br": // line break
            // TODO: MSBUILD does not resolve inheritdoc at build time, we'd have to resolve them ourselves.
            case "inheritdoc":
            {
                // ignored
                break;
            }
        }
    }

    private static string TrimLines(string inputString)
    {
        const StringSplitOptions splitOpts = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        var lines = inputString.Split('\n', splitOpts);
        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryGetChildNode(XmlNode node, string childName, [NotNullWhen(true)] out XmlNode? element)
    {
        element = node[childName];
        return element != null;
    }

    private static bool TryGetAttributeText(XmlNode xmlNode, string attributeName, [NotNullWhen(true)] out string? attributeText)
    {
        attributeText = null;
        var attribute = xmlNode.Attributes?[attributeName];
        if (attribute == null)
            return false;

        attributeText = attribute.InnerText;
        return true;
    }

    /// <summary>
    ///     The text shown when no DocString exists for a key.
    /// </summary>
    public static string Missing(string key)
    {
#if DEBUG
        return $"No DocString defined ({key}).";
#else
        return "No DocString defined.";
#endif
    }
}
