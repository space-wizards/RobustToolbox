using Robust.Packaging.AssetProcessing;

namespace Robust.Packaging;

public sealed class RobustServerPackaging
{
    public static IReadOnlySet<string> ServerIgnoresResources { get; } = new HashSet<string>
    {
        "Textures",
        "Fonts",
        "Shaders",
    };

    public static async Task WriteServerResources(
        string contentDir,
        AssetPass pass,
        CancellationToken cancel = default)
    {
        await WriteServerResources(contentDir, pass, new HashSet<string>(), cancel);
    }

    public static async Task WriteServerResources(
        string contentDir,
        AssetPass pass,
        IReadOnlySet<string> additionalIgnoredResources,
        CancellationToken cancel = default)
    {
        var ignoreSet = ServerIgnoresResources.Union(RobustSharedPackaging.SharedIgnoredResources).ToHashSet();

        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine(contentDir, "Resources"),
            pass,
            ignoreSet.Union(additionalIgnoredResources).ToHashSet(),
            cancel: cancel);

        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine("RobustToolbox", "Resources"),
            pass,
            ignoreSet,
            cancel: cancel);
    }
}
