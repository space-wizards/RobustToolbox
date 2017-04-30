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
            string path = Assembly.GetEntryAssembly().CodeBase;
            path = Path.GetDirectoryName(path);
            return new Uri(path).LocalPath;
        }

        /// <summary>
        /// Get the full path to a file relative to the executable containing directory.
        /// </summary>
        public static string ExecutableRelativeFile(string file)
        {
            return Path.Combine(GetExecutableDirectory(), file);
        }
    }
}
