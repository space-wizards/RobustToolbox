using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Loading;

/// <summary>
/// Simple interface that the replay system loads files from.
/// </summary>
public interface IReplayFileReader : IDisposable
{
    /// <summary>
    /// Check whether a file exists in the replay data.
    /// </summary>
    /// <param name="path">The path to check. Doesn't need to be rooted.</param>
    /// <returns>True if the file exists.</returns>
    bool Exists(ResPath path);

    /// <summary>
    /// Open a file in the replay data.
    /// </summary>
    /// <param name="path">The path to the file. Doesn't need to be rooted.</param>
    /// <returns>A stream containing the file contents.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    Stream Open(ResPath path);

    /// <summary>
    /// Returns all files in the replay data.
    /// </summary>
    /// <remarks>
    /// File paths are rooted.
    /// </remarks>
    IEnumerable<ResPath> AllFiles { get; }
}

/// <summary>
/// Replay file reader that loads files from the VFS (<see cref="IResourceManager"/>).
/// </summary>
public sealed class ReplayFileReaderResources : IReplayFileReader
{
    private readonly IResourceManager _resourceManager;
    private readonly ResPath _prefix;

    /// <param name="resourceManager">The resource manager.</param>
    /// <param name="prefix">The directory in the VFS that contains the replay files. Must be rooted.</param>
    public ReplayFileReaderResources(IResourceManager resourceManager, ResPath prefix)
    {
        _resourceManager = resourceManager;
        _prefix = prefix;
    }

    public bool Exists(ResPath path)
    {
        return _resourceManager.ContentFileExists(GetPath(path));
    }

    public Stream Open(ResPath path)
    {
        return _resourceManager.ContentFileRead(GetPath(path));
    }

    public IEnumerable<ResPath> AllFiles
    {
        get
        {
            foreach (var path in _resourceManager.ContentFindRelativeFiles(_prefix))
            {
                yield return path.ToRelativePath();
            }
        }
    }

    private ResPath GetPath(ResPath path) => _prefix / path.ToRelativePath();

    public void Dispose()
    {
        // Don't need to do anything.
    }
}

/// <summary>
/// Replay file reader that loads files from a zip file.
/// </summary>
/// <remarks>
/// The zip archive is disposed when this instance is disposed.
/// </remarks>
public sealed class ReplayFileReaderZip : IReplayFileReader
{
    private readonly ZipArchive _archive;
    private readonly ResPath _prefix;

    /// <param name="archive">The archive to read files from.</param>
    /// <param name="prefix">The directory in the zip that contains the replay files. Must NOT be rooted.</param>
    public ReplayFileReaderZip(ZipArchive archive, ResPath prefix)
    {
        _archive = archive;
        _prefix = prefix;
    }

    public bool Exists(ResPath path)
    {
        return GetEntry(path) != null;
    }

    public Stream Open(ResPath path)
    {
        var entry = GetEntry(path);
        if (entry == null)
            throw new FileNotFoundException();

        return entry.Open();
    }

    public IEnumerable<ResPath> AllFiles
    {
        get
        {
            foreach (var entry in _archive.Entries)
            {
                // Ignore directories.
                if (entry.FullName.EndsWith("/"))
                    continue;

                var entryPath = new ResPath(entry.FullName);
                if (entryPath.TryRelativeTo(_prefix, out var path))
                    yield return path.Value.ToRootedPath();
            }
        }
    }

    private ZipArchiveEntry? GetEntry(ResPath path) => _archive.GetEntry((_prefix / path.ToRelativePath()).ToString());

    public void Dispose()
    {
        _archive.Dispose();
    }
}
