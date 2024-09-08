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
                        docString = summaryNode.InnerText.Trim();
                    }
                    else
                    {
                        docString = "invalid docstring";
                    }

                    _docStrings.Add(memberName, docString);
                }
            }
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
