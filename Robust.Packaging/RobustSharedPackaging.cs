namespace Robust.Packaging;

public sealed class RobustSharedPackaging
{
    public static IReadOnlySet<string> SharedIgnoredResources { get; } = new HashSet<string>
    {
        "ss13model.7z",
        "ResourcePack.zip",
        "buildResourcePack.py",
        "CONTENT_GOES_HERE",
        ".gitignore",
        ".directory",
        ".DS_Store"
    };
}
