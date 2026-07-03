using System;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.Utility
{
    internal static class UserDataDir
    {
        private static readonly ResPath DataPath = new("/data");

        /// <summary>
        ///     Returns the physical path to the sandboxed user data directory.
        /// </summary>
        public static string GetUserDataDir(IGameControllerInternal gameController)
        {
            var provider = GetUserDataDirProvider(gameController, false);
            return provider.GetFullPath(ResPath.Root);
        }

        /// <summary>
        ///     Returns a writable provider rooted at the user data directory.
        /// </summary>
        public static IWritableDirProvider GetUserDataDirProvider(
            IGameControllerInternal gameController,
            bool hideRootDir)
        {
            var rootProvider = GetRootUserDataDirProvider(gameController, hideRootDir);
            return OpenSubdirectory(rootProvider, DataPath, hideRootDir);
        }

        /// <summary>
        ///     Returns a writable provider rooted at the game-specific user data root.
        /// </summary>
        public static IWritableDirProvider GetRootUserDataDirProvider(
            IGameControllerInternal gameController,
            bool hideRootDir)
        {
            var appDataDir = GetAppDataDir();
            var rootProvider = new WritableDirProvider(Directory.CreateDirectory(appDataDir), hideRootDir);
            var userDataPath = GetUserDataDirectoryNamePath(gameController.Options.UserDataDirectoryName);

            return OpenSubdirectory(rootProvider, userDataPath, hideRootDir);
        }

        private static string GetAppDataDir()
        {
            string appDataDir;

#if LINUX
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (xdgDataHome == null)
            {
                appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            }
            else
            {
                appDataDir = xdgDataHome;
            }
#elif MACOS
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                      "Library", "Application Support");
#else
            appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif

            return appDataDir;
        }

        private static ResPath GetUserDataDirectoryNamePath(ResPath directoryName)
        {
            // UserDataDirectoryName can come from content-level startup code.
            var path = directoryName.ToRootedPath().Clean();
            if (path == ResPath.Root)
                throw new InvalidOperationException("User data directory name must not resolve to the appdata root.");

            return path;
        }

        private static IWritableDirProvider OpenSubdirectory(
            IWritableDirProvider provider,
            ResPath path,
            bool hideRootDir)
        {
            return new WritableDirProvider(Directory.CreateDirectory(provider.GetFullPath(path)), hideRootDir);
        }
    }
}
