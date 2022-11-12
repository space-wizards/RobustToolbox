using Robust.Packaging.AssetProcessing;

namespace Robust.Packaging;

public sealed class RobustClientPackaging
{
    public static IReadOnlySet<string> ClientIgnoresResources { get; } = new HashSet<string>
    {
        "Maps",
        // Leaving this here for future archaeologists to ponder at.
        "emotes.xml",
        "Groups",
        "engineCommandPerms.yml"
    };

    public static async Task WriteClientResources(
        string contentDir,
        AssetPass pass,
        CancellationToken cancel = default)
    {
        var ignoreSet = ClientIgnoresResources.Union(RobustSharedPackaging.SharedIgnoredResources).ToHashSet();

        await RobustSharedPackaging.DoResourceCopy(Path.Combine(contentDir, "Resources"), pass, ignoreSet, cancel);
    }

    public static async Task WriteContentAssemblies(
        AssetPass pass,
        string contentDir,
        string binDir,
        IEnumerable<string> contentAssemblies,
        CancellationToken cancel = default)
    {
        await WriteContentAssemblies("Assemblies", pass, contentDir, binDir, contentAssemblies, cancel);
    }

    public static Task WriteContentAssemblies(
        string target,
        AssetPass pass,
        string contentDir,
        string binDir,
        IEnumerable<string> contentAssemblies,
        CancellationToken cancel = default)
    {
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
            pass.InjectFileFromDisk($"{target}/{f}", Path.Combine(sourceDir, f));
        }

        return Task.CompletedTask;
    }
}
