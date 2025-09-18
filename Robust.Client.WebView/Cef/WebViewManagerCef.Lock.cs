using System;
using System.IO;
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
        var rootDir = Path.Combine(UserDataDir.GetRootUserDataDir(_gameController), BaseCacheName);

        for (var i = 0; i < MaxAttempts; i++)
        {
            var cacheDirPath = Path.Combine(rootDir, i.ToString());

            if (TryLockCacheDir(i, cacheDirPath))
                return cacheDirPath;
        }

        throw new Exception("Unable to locate available CEF cache directory!");
    }

    private bool TryLockCacheDir(int attempt, string path)
    {
        _sawmill.Verbose($"Trying to lock cache directory {attempt}");

        // Does not fail if directory already exists.
        Directory.CreateDirectory(path);

        var lockFilePath = Path.Combine(path, LockFileName);

        try
        {
            var file = File.Open(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            _lockFileStream = file;
            _sawmill.Debug($"Successfully locked CEF cache directory {attempt}");
            return true;
        }
        catch (IOException ex)
        {
            _sawmill.Error($"Failed to lock cache directory {attempt}: {ex}");
            return false;
        }
    }
}
