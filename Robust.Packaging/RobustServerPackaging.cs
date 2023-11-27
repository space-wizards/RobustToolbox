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
        var ignoreSet = ServerIgnoresResources.Union(RobustSharedPackaging.SharedIgnoredResources).ToHashSet();

        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine(contentDir, "Resources"),
            pass,
            ignoreSet,
            cancel: cancel);

        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine("RobustToolbox", "Resources"),
            pass,
            ignoreSet,
            cancel: cancel);
    }
}
