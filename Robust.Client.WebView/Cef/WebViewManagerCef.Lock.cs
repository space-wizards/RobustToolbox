using System;
using System.IO;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.WebView.Cef;

internal sealed partial class WebViewManagerCef
{
    private const string BaseCacheName = "cef_cache";
    private const string LockFileName = "robust.lock";
    private Stream? _lockFileStream;
    private const int MaxAttempts = 15; // This probably shouldn't be a cvar because the only reason you'd need it change for legit just botting the game.

    private string FindAndLockCacheDirectory()
    {
        var userDataRoot = UserDataDir.GetRootUserDataDirProvider(_gameController, true);
        var cacheRootPath = new ResPath($"/{BaseCacheName}");
        userDataRoot.CreateDir(cacheRootPath);

        for (var i = 0; i < MaxAttempts; i++)
        {
            var cacheDirPath = cacheRootPath / i.ToString();

            if (TryLockCacheDir(userDataRoot, i, cacheDirPath))
                return userDataRoot.GetFullPath(cacheDirPath);
        }

        throw new Exception("Unable to locate available CEF cache directory!");
    }

    private bool TryLockCacheDir(IWritableDirProvider userDataRoot, int attempt, ResPath path)
    {
        _sawmill.Verbose($"Trying to lock cache directory {attempt}");

        // Does not fail if directory already exists.
        userDataRoot.CreateDir(path);

        try
        {
            var file = userDataRoot.Open(path / LockFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
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
