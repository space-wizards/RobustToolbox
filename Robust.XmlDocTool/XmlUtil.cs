using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Robust.XmlDocTool;

public static class XmlUtil
{
    public static bool TryGetChildNode(XmlNode node, string childName, [NotNullWhen(true)] out XmlNode? element)
    {
        element = node[childName];
        return element != null;
    }

    public static bool TryGetAttributeText(XmlNode xmlNode, string attributeName, [NotNullWhen(true)] out string? attributeText)
    {
        attributeText = null;
        var attributes = xmlNode.Attributes;

        var attribute = attributes?[attributeName];
        if (attribute == null)
            return false;

        attributeText = attribute.InnerText;
        return true;
    }
}
