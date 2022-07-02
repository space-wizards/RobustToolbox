namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Pipes through any files it receives without modifying them.
/// </summary>
/// <remarks>
/// This can be used to codify standard "ports" on asset graphs.
/// </remarks>
public sealed class AssetPassPipe : AssetPass
{
    /// <summary>
    /// Whether to use <see cref="AssetPass.RunJob"/> to send every file. Parallelizing files at the "start"
    /// </summary>
    public bool Parallelize { get; set; }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
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
