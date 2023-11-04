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
}
