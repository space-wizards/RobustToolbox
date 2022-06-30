namespace Robust.Packaging;

public sealed class RobustClientPackaging
{
    public static IReadOnlySet<string> ClientIgnoresResources { get; } = new HashSet<string>
    {
        "Maps",
        // Leaving this here for future archaeologists to ponder at.
        "emotes.xml",
        "Groups",
        "engineCommandPerms.yml",
        "clientCommandPerms.yml"
    };

    public static IReadOnlySet<string> ClientContentAssemblies { get; } = new HashSet<string>
    {
        "Content.Client",
        "Content.Shared"
    };

    public static async Task WriteClientResources(
        string contentDir,
        IPackageWriter writer,
        CancellationToken cancel = default)
    {
        var ignoreSet = ClientIgnoresResources.Union(RobustSharedPackaging.SharedIgnoredResources).ToHashSet();

        await DoResourceCopy(Path.Combine(contentDir, "Resources"), writer, ignoreSet, cancel);
    }

    public static async Task WriteContentAssemblies(
        IPackageWriter writer,
        string contentDir,
        string binDir,
        IEnumerable<string> contentAssemblies,
        CancellationToken cancel = default)
    {
        await WriteContentAssemblies("Assemblies", writer, contentDir, binDir, contentAssemblies, cancel);
    }

    public static Task WriteContentAssemblies(
        string target,
        IPackageWriter writer,
        string contentDir,
        string binDir,
        IEnumerable<string> contentAssemblies,
        CancellationToken cancel = default)
    {
        EnsureDirNode(writer, $"{target}/");

        var files = new List<string>();

        var sourceDir = Path.Combine(contentDir, "bin", binDir);

        foreach (var asm in contentAssemblies)
        {
            files.Add($"{asm}.dll");

            var pdbPath = $"{asm}.pdb";
            if (File.Exists(Path.Combine(sourceDir, pdbPath)))
                files.Add(pdbPath);
        }

        foreach (var f in files)
        {
            cancel.ThrowIfCancellationRequested();
            writer.WriteResourceFromDisk($"{target}/{f}", Path.Combine(sourceDir, f));
        }

        return Task.CompletedTask;
    }

    public static Task DoResourceCopy(
        string diskSource,
        IPackageWriter writer,
        IReadOnlySet<string> ignoreSet,
        CancellationToken cancel = default)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(diskSource))
        {
            cancel.ThrowIfCancellationRequested();

            var filename = Path.GetFileName(path);
            if (ignoreSet.Contains(filename))
                continue;

            var targetPath = filename;
            if (Directory.Exists(path))
                CopyDirIntoZip(path, targetPath, writer);
            else
                writer.WriteResourceFromDisk(targetPath, path);
        }

        return Task.CompletedTask;
    }

    private static void CopyDirIntoZip(string directory, string basePath, IPackageWriter archive)
    {
        if (basePath.Length > 0)
            EnsureDirNode(archive, $"{basePath}/");

        foreach (var (root, files) in DirectoryWalk(directory))
        {
            var relPath = Path.GetRelativePath(directory, root);
            if (Path.DirectorySeparatorChar != '/')
                relPath = relPath.Replace(Path.DirectorySeparatorChar, '/');

            if (relPath != ".")
            {
                EnsureDirNode(archive, $"{basePath}/{relPath}/");
            }

            foreach (var filename in files)
            {
                var zipPath = relPath == "." ? $"{basePath}/{filename}" : $"{basePath}/{relPath}/{filename}";
                var filePath = Path.Combine(root, filename);

                Console.WriteLine($"{directory}/{zipPath} -> /{zipPath}");
                archive.WriteResourceFromDisk(zipPath, filePath);
            }
        }
    }

    private static IEnumerable<(string root, string[] files)> DirectoryWalk(string path)
    {
        yield return (path, Directory.GetFiles(path).Select(Path.GetFileName).ToArray())!;
        foreach (var dirPath in Directory.GetDirectories(path))
        {
            var dirName = Path.GetFileName(dirPath);

            foreach (var ret in DirectoryWalk(Path.Combine(path, dirName)))
            {
                yield return ret;
            }
        }
    }

    private static void EnsureDirNode(IPackageWriter zip, string path)
    {
        // TODO: Necessary?
        return;
        /*
        if (zip.GetEntry(path) != null)
            return;

        zip.CreateEntry(path, CompressionLevel.NoCompression);
        */
    }
}
