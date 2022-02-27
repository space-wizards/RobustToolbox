namespace Robust.Shared.ContentPack;

/// <summary>
/// Seekability force mode for ResourceManager.
/// </summary>
public enum StreamSeekMode
{
    /// <summary>
    /// Do not do anything special. Streams will be seekable if the VFS can provide it (e.g. not compressed).
    /// </summary>
    None = 0,

    /// <summary>
    /// All streams will be forced as seekable by buffering them in memory if necessary.
    /// </summary>
    ForceSeekable = 1,

    /// <summary>
    /// Force streams to be non-seekable by wrapping them in another stream instances.
    /// </summary>
    ForceNonSeekable = 2
}
