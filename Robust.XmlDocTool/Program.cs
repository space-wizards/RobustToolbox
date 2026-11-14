using System.Diagnostics;
using System.Xml;

using Robust.XmlDocTool;

//
// XmlDocTool
// A utility program for reducing the size of the generated xml docs for use with
// the ViewVariables utility inside Robust games.
//

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            // give us a target string, idiot
            Console.WriteLine("XmlDocTool - Needs folder or file path!");
            return 1;
        }

        var targetString = args[0];
        IEnumerable<string> filePaths;

        if (File.Exists(targetString))
        {
            filePaths = [targetString];
        }
        else if (Directory.Exists(targetString))
        {
            filePaths = Directory.GetFiles(targetString, "*.xml");
        }
        else
        {
            Console.WriteLine($"XmlDocTool - Argument 1 `{targetString}` is not a valid file or directory!");
            return 1;
        }

        var results = filePaths.AsParallel()
            .Select(ProcessFile)
            .ToArray();
        return results.All(r => r) ? 0 : 1;
    }

    public static bool ProcessFile(string filePath)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(filePath);

        // load the whole source doc into memory and release the file so we can overwrite it at the end
        var sourceXmlDoc = new XmlDocument();
        using (var sourceFileStream = File.OpenRead(filePath))
        {
            try
            {
                sourceXmlDoc.Load(sourceFileStream);
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"XmlDocTool - DocString xml file at `{filePath}` failed to load with exception: {ex}");
                return false;
            }
        }

        if (!XmlUtil.TryGetChildNode(sourceXmlDoc, "doc", out var sourceDocNode))
        {
            Console.WriteLine($"XmlDocTool - DocString xml file at `{filePath}` lacks `doc` node!");
            return false;
        }

        // create the destination doc

        var destXmlDoc = new XmlDocument();

        var destDocNode = destXmlDoc.CreateElement("doc");
        destXmlDoc.AppendChild(destDocNode);

        // import the <assembly> block from source to dest

        if (!XmlUtil.TryGetChildNode(sourceDocNode, "assembly", out var sourceAssemblyNode))
        {
            Console.WriteLine($"XmlDocTool - DocString xml file at `{filePath}` lacks `assembly` node!");
            return false;
        }

        var importedAssemblyNode = destXmlDoc.ImportNode(sourceAssemblyNode, true);
        destDocNode.AppendChild(importedAssemblyNode);

        // start importing the member nodes

        if (!XmlUtil.TryGetChildNode(sourceDocNode, "members", out var sourceMembersNode))
        {
            Console.WriteLine($"XmlDocTool - DocString xml file at `{filePath}` lacks `members` node!");
            return false;
        }

        var destMembersNode = destXmlDoc.CreateElement("members");
        destDocNode.AppendChild(destMembersNode);

        var totalNodes = 0;
        var copiedNodes = 0;
        foreach (XmlNode memberNode in sourceMembersNode.ChildNodes)
        {
            totalNodes++;

            // skips comment blocks, whitespace between elements
            if (memberNode.NodeType != XmlNodeType.Element)
                continue;

            Debug.Assert(memberNode.Name == "member");

            if (!XmlUtil.TryGetAttributeText(memberNode, "name", out var memberName))
                continue;

            // You might be tempted to discard more unnecessary data or
            // pre-process some of the text now instead of when the game is
            // running. That's all premature at this point, the space saved
            // by discarding method definitions is more then enough for now.

            // skip method definitions
            if (memberName.StartsWith("M:"))
                continue;

            // clone the member node to the destination doc
            var importedMemberNode = destXmlDoc.ImportNode(memberNode, true);
            destMembersNode.AppendChild(importedMemberNode);

            copiedNodes++;
        }

        var writerSettings = new XmlWriterSettings();
        writerSettings.Indent = true;
        writerSettings.IndentChars = "    ";

        using var xmlWriter = XmlWriter.Create(filePath, writerSettings);
        destXmlDoc.Save(xmlWriter);

        Console.WriteLine($"XmlDocTool - {sourceFileName}: Total Nodes: {totalNodes}, Copied Nodes: {copiedNodes}, Delta: {copiedNodes - totalNodes}");
        return true;
    }
}
