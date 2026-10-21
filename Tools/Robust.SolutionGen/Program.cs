using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

var targetOption = new Option<FileInfo>("--solution", "-s")
{
    Description =
        "Game solution file to update. If not provided, a .slnx file in the current directory is located automatically."
}.AcceptExistingOnly();

var outputOption = new Option<FileInfo>("--output", "-o")
{
    Description = "If provided, output to a new file instead of updating in-place."
}.AcceptLegalFilePathsOnly();

var robustOptions = new Option<FileInfo>("--robust")
{
    DefaultValueFactory = _ => new FileInfo("RobustToolbox"),
    Description = "Path to RobustToolbox"
}.AcceptExistingOnly();

var updateCommand = new Command("update");
updateCommand.Description = "Update your game's solution file to be compatible with this RT version.";
updateCommand.Add(targetOption);
updateCommand.Add(robustOptions);
updateCommand.Add(outputOption);
updateCommand.SetAction(CmdUpdate);

var rootCommand = new RootCommand("Robust.SolutionGen") { updateCommand };
return rootCommand.Parse(args).Invoke();

void CmdUpdate(ParseResult result)
{
    var rtSlnx = Path.Combine(result.GetRequiredValue(robustOptions).FullName, "RobustToolbox.slnx");
    var source = GetSolutionTarget(result);
    var target = result.GetValue(outputOption) ?? source;

    var rtDocument = new XmlDocument();
    rtDocument.Load(rtSlnx);
    var rtRoot = rtDocument.DocumentElement;
    if (rtRoot is not { Name: "Solution" })
        Bail("Invalid RT solution file: does not start with Solution element");

    var sourceDocument = new XmlDocument();
    sourceDocument.Load(source.FullName);

    var root = sourceDocument.DocumentElement;
    if (root is not { Name: "Solution" })
        Bail("Invalid solution file: does not start with Solution element");

    var features = GetFeatures(root);

    RemoveRobustEntries(root);
    MergeRobustSolution(sourceDocument, root, rtRoot, features);

    sourceDocument.Save(target.FullName);
}

void RemoveRobustEntries(XmlElement root)
{
    var toRemove = new List<XmlElement>();
    foreach (XmlElement folder in root.GetElementsByTagName("Folder"))
    {
        var nameAttr = folder.GetAttribute("Name");
        if (nameAttr.StartsWith("/RobustToolbox/"))
            toRemove.Add(folder);
    }

    toRemove.ForEach(el => root.RemoveChild(el));
}

void MergeRobustSolution(XmlDocument targetDocument, XmlElement targetElement, XmlElement rtElement, string[] features)
{
    var rootFolder = targetDocument.CreateElement("Folder");
    var nameAttr = targetDocument.CreateAttribute("Name");
    nameAttr.Value = "/RobustToolbox/";
    rootFolder.Attributes.Append(nameAttr);
    foreach (var elem in rtElement.ChildNodes)
    {
        if (elem is XmlElement { Name: "Folder" } folder)
        {
            var folderClone = MapFolder(targetDocument, folder, features);
            if (folderClone != null)
                targetElement.AppendChild(folderClone);
        }
        else if (elem is XmlElement { Name: "Project" } project)
        {
            var mappedProject = MapProject(targetDocument, project, features);
            if (mappedProject != null)
                rootFolder.AppendChild(mappedProject);
        }
    }

    var solutionFile = targetDocument.CreateElement("File");
    solutionFile.SetAttribute("Path", "RobustToolbox/RobustToolbox.slnx");
    rootFolder.AppendChild(solutionFile);

    targetElement.AppendChild(rootFolder);
}

bool IsFeatureEnabled(XmlElement rtElement, string[] features)
{
    var config = rtElement.SelectSingleNode("Properties[@Name=\"RobustToolbox\"]/Property[@Name=\"Feature\"]/@Value");
    if (config is not { Value: { } value })
        return true;

    return features.Contains(value);
}

XmlElement? MapFolder(XmlDocument targetDocument, XmlElement rtElement, string[] features)
{
    var clone = (XmlElement)targetDocument.ImportNode(rtElement, false);
    clone.SetAttribute("Name", "/RobustToolbox" + clone.GetAttribute("Name"));

    foreach (var elem in rtElement.ChildNodes)
    {
        if (elem is XmlElement { Name: "Project" } project)
        {
            var mappedProject = MapProject(targetDocument, project, features);
            if (mappedProject != null)
                clone.AppendChild(mappedProject);
        }
        else if (elem is XmlElement { Name: "File" } file)
        {
            clone.AppendChild(MapFile(targetDocument, file));
        }
    }

    return clone.HasChildNodes ? clone : null;
}

XmlElement? MapProject(XmlDocument targetDocument, XmlElement rtElement, string[] features)
{
    if (!IsFeatureEnabled(rtElement, features))
        return null;

    var clone = (XmlElement)targetDocument.ImportNode(rtElement, true);
    clone.SetAttribute("Path", "RobustToolbox/" + clone.GetAttribute("Path"));
    return clone;
}

XmlElement MapFile(XmlDocument targetDocument, XmlElement rtElement)
{
    var clone = (XmlElement)targetDocument.ImportNode(rtElement, true);
    clone.SetAttribute("Path", "RobustToolbox/" + clone.GetAttribute("Path"));
    return clone;
}

FileInfo GetSolutionTarget(ParseResult result)
{
    var targetGiven = result.GetValue(targetOption);

    if (targetGiven != null)
        return targetGiven;

    var root = Environment.CurrentDirectory;
    var candidates = Directory.GetFiles(root, "*.slnx");
    if (candidates.Length > 1)
        Bail("There are multiple .slnx files in this directory, please specify the path directory with --solution.");

    if (candidates.Length == 0)
        Bail("There are no .slnx files in this directory, please specify the path directory with --solution");

    return new FileInfo(Path.Combine(root, candidates[0]));
}

string[] GetFeatures(XmlElement root)
{
    var config = root.SelectSingleNode("Properties[@Name=\"RobustToolbox\"]/Property[@Name=\"Features\"]/@Value");
    if (config is not { Value: { } value })
        return [];

    return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

[DoesNotReturn]
void Bail(string message)
{
    Console.WriteLine(message);
    Environment.Exit(1);
}
