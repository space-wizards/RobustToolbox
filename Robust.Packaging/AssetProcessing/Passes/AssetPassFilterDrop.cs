namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Asset pass that drops all files that match a predicate. Files that do not match are ignored.
/// </summary>
public sealed class AssetPassFilterDrop(Func<AssetFile, bool> predicate) : AssetPass
{
    public Func<AssetFile, bool> Predicate { get; } = predicate;

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        // Just do nothing with the file so it gets discarded.
        if (Predicate(file))
            return AssetFileAcceptResult.Consumed;

        return base.AcceptFile(file);
    }
}
