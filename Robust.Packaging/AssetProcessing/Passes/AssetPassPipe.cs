namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Pipes through any files it receives without modifying them.
/// </summary>
/// <remarks>
/// This can be used to codify standard "ports" on asset graphs.
/// </remarks>
public sealed class AssetPassPipe : AssetPass
{
    private readonly HashSet<string> _passedAssetNames = new();

    /// <summary>
    /// Whether to use <see cref="AssetPass.RunJob"/> to send every file. Parallelizing files at the "start"
    /// </summary>
    public bool Parallelize { get; set; }

    /// <summary>
    /// If set, this pass will drop files with a duplicate path that pass through it.
    /// </summary>
    /// <remarks>
    /// This is always considered an error, even when this flag is not set.
    /// This flag is simply to avoid the overhead by default.
    /// </remarks>
    public bool CheckDuplicates { get; set; }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        if (CheckDuplicates)
        {
            bool added;
            lock (_passedAssetNames)
            {
                added = _passedAssetNames.Add(file.Path);
            }

            if (!added)
            {
                Logger?.Error($"{Name}: Duplicate file detected, will be skipped: '{file.Path}'");
                return AssetFileAcceptResult.Consumed;
            }
        }

        if (Parallelize)
        {
            RunJob(() => SendFile(file));
        }
        else
        {
            SendFile(file);
        }

        return AssetFileAcceptResult.Consumed;
    }
}
