using Robust.Packaging.AssetProcessing;

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
        var ignoreSet = ClientIgnoredResources.Union(RobustSharedPackaging.SharedIgnoredResources).ToHashSet();

        await RobustSharedPackaging.DoResourceCopy(Path.Combine(contentDir, "Resources"), pass, ignoreSet, cancel: cancel);
    }
}
