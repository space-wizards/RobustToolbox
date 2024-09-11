using System.Xml;

namespace XmlDocTool;

public static class XmlDocRoundTripTest
{
    //
    // The basic conclusion I can draw from this test is that I probablly /could/
    // make the output xml docs be as binary-simmilar to the input docs as possible ...
    //
    // But it would take a while, be annoying, and what the process creates currently is
    // good enough.
    //

    public static void ProcessFile(string filePath)
    {
        var sourceFileDirectory = Path.GetDirectoryName(filePath);
        var sourceFileName = Path.GetFileNameWithoutExtension(filePath);

        if (sourceFileName.Contains("temp"))
            return;

        using var sourceFileStream = File.OpenRead(filePath);

        var sourceXmlDoc = new XmlDocument();
        try
        {
            sourceXmlDoc.Load(sourceFileStream);
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"DocString xml file at `{filePath}` failed to load with exception: {ex}");
            return;
        }

        if (!XmlUtil.TryGetChildNode(sourceXmlDoc, "doc", out var sourceDocNode))
        {
            Console.WriteLine($"DocString xml file at `{filePath}` lacks `doc` node!");
            return;
        }

        // create the destination doc

        var destXmlDoc = new XmlDocument();

        var destDocNode = destXmlDoc.CreateElement("doc");
        destXmlDoc.AppendChild(destDocNode);

        // import the <assembly> block from source to dest

        if (!XmlUtil.TryGetChildNode(sourceDocNode, "assembly", out var sourceAssemblyNode))
        {
            Console.WriteLine($"DocString xml file at `{filePath}` lacks `assembly` node!");
            return;
        }

        var importedAssemblyNode = destXmlDoc.ImportNode(sourceAssemblyNode, true);
        destDocNode.AppendChild(importedAssemblyNode);

        // start importing the member nodes

        if (!XmlUtil.TryGetChildNode(sourceDocNode, "members", out var sourceMembersNode))
        {
            Console.WriteLine($"DocString xml file at `{filePath}` lacks `members` node!");
            return;
        }

        var destMembersNode = destXmlDoc.CreateElement("members");
        destDocNode.AppendChild(destMembersNode);

        foreach (XmlNode memberNode in sourceMembersNode.ChildNodes)
        {
            var importedMemberNode = destXmlDoc.ImportNode(memberNode, true);
            destMembersNode.AppendChild(importedMemberNode);
        }

        var tempFilename = $"{sourceFileDirectory}\\{sourceFileName}.temp.xml";
        var writerSettings = new XmlWriterSettings();
        writerSettings.Indent = true;
        writerSettings.IndentChars = "    ";

        using (var xmlWriter = XmlWriter.Create(tempFilename, writerSettings))
            destXmlDoc.Save(xmlWriter);
    }
}
