using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Utility functions
    /// </summary>
    public static class PathHelpers
    {
        /// <summary>
        ///     Get the full directory path that the executable is located in.
        /// </summary>
        private static string GetExecutableDirectory()
        {
            // TODO: remove this shitty hack, either through making it less hardcoded into shared,
            //   or by making our file structure less spaghetti somehow.
            // Godot always executes relative to the project.godot file you opened the engine/editor on.
            // So this needs to return the directory relative to SS14.Client.dll,
            //   NOT the current working dir, when on the client.
            var assembly = AppDomain.CurrentDomain.GetAssemblyByName("SS14.Client")
                           ?? Assembly.GetEntryAssembly()
                           ?? Assembly.GetExecutingAssembly();
            var path = new Uri(assembly.CodeBase).LocalPath;
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
    }
}
