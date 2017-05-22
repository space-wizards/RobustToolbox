using System.IO;
using System;
using System.Reflection;

namespace SS14.Shared.Utility
{
    public static class PathHelpers
    {
        /// <summary>
        /// Get the full directory path that the executable is located in.
        /// </summary>
        public static string GetExecutableDirectory()
        {
            string path = new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Get the full path to a file relative to the executable containing directory.
        /// </summary>
        public static string ExecutableRelativeFile(string file)
        {
            return Path.Combine(GetExecutableDirectory(), file);
        }

        /// <summary>
        /// Ensures that the path from the current executable exists.
        /// </summary>
        public static DirectoryInfo EnsureRelativePath(string path)
        {
            return Directory.CreateDirectory(ExecutableRelativeFile(path));
        }
    }
}
