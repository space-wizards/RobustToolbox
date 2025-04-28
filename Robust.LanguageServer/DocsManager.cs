using System.Reflection;
using System.Xml;
using LoxSmoke.DocXml;

namespace Robust.LanguageServer;

public sealed class DocsManager
{
    internal static HashSet<Assembly> _loadedAssemblies = new HashSet<Assembly>();

    private DocXmlReader _reader;

    public DocsManager()
    {
        _reader = new DocXmlReader(GetAssemblyXmlPath);
    }

    public static string GetAssemblyXmlPath(Assembly assembly)
    {
        var result = Path.ChangeExtension(assembly.Location, ".xml");
        Console.WriteLine($"Getting ext for {assembly} -> {result}");
        return result;
    }

    public TypeComments GetComments(Type type)
    {
        return _reader.GetTypeComments(type);
    }

    public CommonComments GetComments(MemberInfo member)
    {
        return _reader.GetMemberComments(member);
    }

    public void Initialize()
    {

    }

    // internal static void LoadXmlDocumentation(Assembly assembly)
    // {
    //     if (_loadedAssemblies.Contains(assembly)) {
    //         return; // Already loaded
    //     }
    //     string directoryPath = GetDirectoryPath(assembly);
    //     string xmlFilePath = Path.Combine(directoryPath, assembly.GetName().Name + ".xml");
    //     if (File.Exists(xmlFilePath)) {
    //         LoadXmlDocumentation(File.ReadAllText(xmlFilePath));
    //         _loadedAssemblies.Add(assembly);
    //     }
    // }
    //
    // internal static Dictionary<string, string> loadedXmlDocumentation = new();
    //
    // public static void LoadXmlDocumentation(string xmlDocumentation)
    // {
    //     using (XmlReader xmlReader = XmlReader.Create(new StringReader(xmlDocumentation)))
    //     {
    //         while (xmlReader.Read())
    //         {
    //             if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "member")
    //             {
    //                 if (xmlReader["name"] is {} rawName)
    //                     loadedXmlDocumentation[rawName] = xmlReader.ReadInnerXml();
    //             }
    //         }
    //     }
    // }
    //
    // internal static string GetDirectoryPath(Assembly assembly)
    // {
    //     string codeBase = assembly.CodeBase;
    //     UriBuilder uri = new UriBuilder(codeBase);
    //     string path = Uri.UnescapeDataString(uri.Path);
    //     return Path.GetDirectoryName(path);
    // }
}
