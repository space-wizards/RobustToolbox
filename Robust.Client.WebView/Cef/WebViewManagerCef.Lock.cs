using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Client.Utility;

namespace Robust.Client.WebView.Cef;

internal sealed partial class WebViewManagerCef
{
    private const string BaseCacheName = "cef_cache";
    private const string LockFileName = "robust.lock";
    private FileStream? _lockFileStream;
    private const int MaxAttempts = 15; // This probably shouldn't be a cvar because the only reason you'd need it change for legit just botting the game.

    private string FindAndLockCacheDirectory()
    {
        var finalAbsoluteCachePath = "";

        var rootDir = Path.Combine(UserDataDir.GetRootUserDataDir(_gameController), BaseCacheName);

        try
        {
            var existingCacheDirs = GetExistingCacheDirectories(rootDir);
            existingCacheDirs.Sort();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var relativeDirName in existingCacheDirs)
            {
                var absoluteDirPath = Path.Combine(rootDir, relativeDirName);

                if (!Directory.Exists(absoluteDirPath)
                    || !TryAcquireDirectoryLock(absoluteDirPath, out FileStream? lockStream)) continue;

                _lockFileStream = lockStream;
                finalAbsoluteCachePath = absoluteDirPath;
                _sawmill.Debug($"Found and locked existing cache directory: {finalAbsoluteCachePath}");
                break;
            }

            if (string.IsNullOrEmpty(finalAbsoluteCachePath))
                finalAbsoluteCachePath = CreateLockNewCacheDir(rootDir);

            return finalAbsoluteCachePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to find or create cache directory", ex);
        }
    }

    private List<string> GetExistingCacheDirectories(string rootDir)
    {
        List<string> existingCacheDirs = new();

        try
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var entryName in Directory.EnumerateDirectories(rootDir))
                if (Directory.Exists(Path.Combine(rootDir, entryName)))
                    existingCacheDirs.Add(entryName);
        }
        catch (IOException ex)
        {
            _sawmill.Warning($"Failed to enumerate cache directories: {ex.Message}");
        }

        return existingCacheDirs;
    }

    private bool TryAcquireDirectoryLock(string directoryPath, [NotNullWhen(true)] out FileStream? lockStream)
    {
        lockStream = null;
        var lockFilePath = Path.Combine(directoryPath, LockFileName);

        try
        {
            if (File.Exists(lockFilePath))
            {
                if (IsLockFileValid(lockFilePath))
                {
                    _sawmill.Debug($"Cache directory {directoryPath} is locked by active process");
                    return false;
                }

                _sawmill.Debug($"Removing stale lock file: {lockFilePath}");
                File.Delete(lockFilePath);
            }

            lockStream = new FileStream(lockFilePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            return true;
        }
        catch (IOException)
        {
            lockStream?.Dispose();
            lockStream = null;
            return false;
        }
    }

    // Check if this file is actually locked
    private bool IsLockFileValid(string lockFilePath)
    {
        try
        {
            using FileStream testStream = new(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private string CreateLockNewCacheDir(string rootDir)
    {
        for (var attempts = 0; attempts < MaxAttempts; attempts++)
        {
            var newRelativeCacheDir = attempts.ToString();
            var absolutePath = Path.Combine(rootDir, newRelativeCacheDir);

            try
            {
                if (!TryCreateLockDir(absolutePath, out var lockStream))
                    continue;

                _lockFileStream = lockStream;
                _sawmill.Debug($"Created and locked new cache directory: {absolutePath}");
                return absolutePath;
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Failed to create directory {absolutePath}: {ex.Message}");
            }
        }

        throw new InvalidOperationException($"Failed to create any cache directory after {MaxAttempts} attempts");
    }

    private bool TryCreateLockDir(string directoryPath, [NotNullWhen(true)] out FileStream? lockStream)
    {
        lockStream = null;
        var lockFilePath = Path.Combine(directoryPath, LockFileName);

        try
        {
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            lockStream = new FileStream(lockFilePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            return true;
        }
        catch (IOException)
        {
            lockStream?.Dispose();
            lockStream = null;

            try
            {
                if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    Directory.Delete(directoryPath);
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}
