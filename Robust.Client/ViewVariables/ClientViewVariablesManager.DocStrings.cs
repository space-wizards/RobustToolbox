using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables
{
    internal sealed partial class ClientViewVariablesManager
    {
        public void LoadDocStrings()
        {
            foreach (var resPath in _resManager.ContentFindFiles("/ViewVariables/"))
            {
                using var resStream = _resManager.ContentFileRead(resPath);

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(resStream);

                var docNode = xmlDoc["doc"];
                var membersNode = docNode!["members"];
                foreach (XmlNode memberNode in membersNode!.ChildNodes)
                {
                    if (memberNode.NodeType != XmlNodeType.Element)
                        continue;

                    DebugTools.Assert(memberNode.Name == "member");

                    if (memberNode.Attributes == null)
                        continue;

                    XmlAttribute? nameAttribute = memberNode.Attributes["name"];
                    var memberName = nameAttribute!.InnerText;
                    var summaryNode = memberNode["summary"];

                    string docString;
                    if (summaryNode != null)
                    {
                        var xmlString = ProcessDocNode(summaryNode);
                        docString = TrimLines(xmlString);
                    }
                    else
                    {
                        docString = "invalid docstring";
                    }

                    _docStrings.Add(memberName, docString);
                }
            }
        }

        private string ProcessDocNode(XmlNode xmlNode)
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

        private void ProcessXmlElement(XmlNode elementNode, StringBuilder sb)
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
                case "code":  // use code formatting
                case "c":     //
                case "para":  // paragraph text
                case "value": //
                {
                    DebugTools.Assert(!string.IsNullOrEmpty(elementNode.InnerText));
                    sb.Append(elementNode.InnerText);
                    break;
                }
                case "br":
                {
                    // line break
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
                case "inheritdoc":
                {
                    break;
                }
            }
        }

        private static string TrimLines(string inputString)
        {
            var splitOpts = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
            var lines = inputString.Split('\n', splitOpts);
            return string.Join(Environment.NewLine, lines);
        }

        private static bool TryGetAttributeText(XmlNode xmlNode, string attributeName, [NotNullWhen(true)] out string? attributeText)
        {
            attributeText = null;
            var attributes = xmlNode.Attributes;
            if (attributes != null)
            {
                var attribute = attributes[attributeName];
                if (attribute != null)
                {
                    attributeText = attribute.InnerText;
                    return true;
                }
            }
            return false;
        }

        public string GetDocStringForType(string key)
        {
            if (_docStrings.TryGetValue($"T:{key}", out string? docString))
            {
                return docString;
            }
            else
            {
#if DEBUG
                return $"docstring not found ({key})";
#else
                return "docstring not found";
#endif
            }
        }

        public string GetDocStringForFieldOrProperty(string key)
        {
            // Will lumping fields and properties into the same search come back to bite us? Yes!
            // It's a problem for future someone to take care of. You'll have to make the server
            // send over a bool that toggles if we should look for a field or a property.
            if (_docStrings.TryGetValue($"F:{key}", out string? fieldDoc))
            {
                return fieldDoc;
            }
            if (_docStrings.TryGetValue($"P:{key}", out string? propDoc))
            {
                return propDoc;
            }
            else
            {
#if DEBUG
                return $"docstring not found ({key})";
#else
                return "docstring not found";
#endif
            }
        }
    }
}
