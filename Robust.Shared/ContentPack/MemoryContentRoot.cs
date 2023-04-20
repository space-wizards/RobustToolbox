using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack;

/// <summary>
///     A content root stored in memory, backed by a dictionary.
/// </summary>
public sealed class MemoryContentRoot : IContentRoot, IDisposable
{
    private readonly Dictionary<ResourcePath, byte[]> _files = new();

    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    ///     Adds a file to the content root, or updates it if that path already exists.
    /// </summary>
    /// <param name="relPath">The relative path of the file.</param>
    /// <param name="data">The data byte array to store in the content root. Stored as is, without being copied or cloned.</param>
    public void AddOrUpdateFile(ResourcePath relPath, byte[] data)
    {
        // Just in case, we ensure it's a clean relative path.
        relPath = relPath.Clean().ToRelativePath();

        _lock.EnterWriteLock();
        try
        {
            _files[relPath] = data;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     Remove a file from this content root.
    /// </summary>
    /// <param name="relPath">The relative path to the file.</param>
    /// <returns></returns>
    public bool RemoveFile(ResourcePath relPath)
    {
        _lock.EnterWriteLock();
        try
        {
            return _files.Remove(relPath);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public bool TryGetFile(ResourcePath relPath, [NotNullWhen(true)] out Stream? stream)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_files.TryGetValue(relPath, out var data))
            {
                stream = null;
                return false;
            }

            // Non-writable stream, as this needs to be thread-safe.
            stream = new MemoryStream(data, false);
            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
    {
        _lock.EnterReadLock();
        try
        {
            foreach (var (file, _) in _files)
            {
                if (file.TryRelativeTo(path, out _))
                    yield return file;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRelativeFilePaths()
    {
        _lock.EnterReadLock();
        try
        {
            foreach (var (file, _) in _files)
            {
                yield return file.ToString();
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     Enumerates all files and their resource paths on this content root.
    /// </summary>
    /// <remarks>Do not modify or keep around the returned byte array, it's meant to be read-only.</remarks>
    public IEnumerable<(ResourcePath relPath, byte[] data)> GetAllFiles()
    {
        _lock.EnterReadLock();
        try
        {
            foreach (var (p, d) in _files)
            {
                yield return (p, d);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public void Mount()
    {
        // Nada. We don't need to perform any special logic here.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
    }
}
