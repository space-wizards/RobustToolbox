using Robust.Packaging.AssetProcessing;
using Robust.Shared.ContentPack;

namespace Robust.Packaging;

public sealed class RobustClientPackaging
{
    public static IReadOnlySet<string> ClientIgnoredResources { get; } = new HashSet<string>
    {
        "Maps",
        "ConfigPresets",
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
        await WriteClientResources(contentDir, pass, new HashSet<string>(), cancel);
    }

    public static async Task WriteClientResources(
        string contentDir,
        AssetPass pass,
        IReadOnlySet<string> additionalIgnoredResources,
        CancellationToken cancel = default)
    {
        var ignoreSet = ClientIgnoredResources
            .Union(RobustSharedPackaging.SharedIgnoredResources)
            .Union(additionalIgnoredResources)
            .ToHashSet();

        await RobustSharedPackaging.DoResourceCopy(Path.Combine(contentDir, "Resources"), pass, ignoreSet, cancel: cancel);

        var manifestPath = Path.Combine(contentDir, "Resources", "manifest.yml");
        var manifest = ResourceManifestData.LoadFromFile(manifestPath); // load from disk no VFS

        if (manifest.ModularResources != null)
        {
            foreach (var (vfsPath,diskName) in manifest.ModularResources)
            {
                var modPath = Path.Combine(contentDir, diskName);
                if (!Directory.Exists(modPath))
                    continue;
                var files = Directory.EnumerateFiles(modPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var filename = Path.GetFileName(file);
                    if (ignoreSet.Contains(filename)) continue;
                    var relative = Path.GetRelativePath(modPath, file);
                    var zipRoot = vfsPath.TrimStart('/');
                    var targetPath = Path.Combine(zipRoot, relative).Replace('\\', '/');
                    await using var stream = File.OpenRead(file);
                    pass.InjectFileFromDisk(targetPath, file);
                }
            }
        }
    }
}
