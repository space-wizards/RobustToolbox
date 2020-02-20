using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private static string GetExecutableDirectory()
        {
            // TODO: remove this shitty hack, either through making it less hardcoded into shared,
            //   or by making our file structure less spaghetti somehow.
            var assembly = typeof(PathHelpers).Assembly;
            var pathUri = new Uri(assembly.CodeBase);
            var path = pathUri.LocalPath;
            if (pathUri.Fragment != "")
            {
                path += pathUri.Fragment;
            }
            return Path.GetDirectoryName(path);
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
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();

                foreach (var subDir in Directory.GetDirectories(path))
                {
                    queue.Enqueue(subDir);
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    yield return file;
                }
            }
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

    }
}
