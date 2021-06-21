using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Utility functions
    /// </summary>
    internal static class PathHelpers
    {
        /// <summary>
        ///     Get the full directory path that the executable is located in.
        /// </summary>
        internal static string GetExecutableDirectory()
        {
            // TODO: remove this shitty hack, either through making it less hardcoded into shared,
            //   or by making our file structure less spaghetti somehow.
            var assembly = typeof(PathHelpers).Assembly;
            var location = assembly.Location;
            if (location == string.Empty)
            {
                // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location?view=net-5.0#remarks
                // This doesn't apply to us really because we don't do that kind of publishing, but whatever.
                throw new InvalidOperationException("Cannot find path of executable.");
            }
            return Path.GetDirectoryName(location)!;
        }

        /// <summary>
        ///     Turns a relative path from the executable directory into a full path.
        /// </summary>
        public static string ExecutableRelativeFile(string file)
        {
            return Path.GetFullPath(Path.Combine(GetExecutableDirectory(), file));
        }

        /// <summary>
        ///     Recursively gets all files in a directory and all sub directories.
        /// </summary>
        /// <param name="path">Directory to start in.</param>
        /// <returns>Enumerable of all file paths in that directory and sub directories.</returns>
        public static IEnumerable<string> GetFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        public static bool IsFileInUse(IOException exception)
        {
            var errorCode = exception.HResult & 0xFFFF;
            return errorCode switch
            {
                // TODO: verify works on non-win systems
                32 => /* sharing not allowed */ true,
                33 => /* file is locked */ true,
                _ => false
            };
        }

        // TODO: gaf
        public static bool IsFileSystemCaseSensitive() =>
            !OperatingSystem.IsWindows()
            && !OperatingSystem.IsMacOS();

    }
}
