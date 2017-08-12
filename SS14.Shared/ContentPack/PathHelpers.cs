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
        public static string GetExecutableDirectory()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
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

        //TODO: What is this supposed to be doing... path isn't even used... FIX ME
        public static string AssemblyRelativeFile(string file, Assembly assembly)
        {
            var path = new Uri(assembly.CodeBase).LocalPath;
            return Path.Combine(GetExecutableDirectory(), file);
        }

        /// <summary>
        ///     Recursively gets all files in a directory and all sub directories.
        /// </summary>
        /// <param name="path">Directory to start in.</param>
        /// <returns>Enumerable of all file paths in that directory and sub directories.</returns>
        public static IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, e => Console.WriteLine(e));
        }

        //TODO:  onError isn't used... FIX ME
        // Source: http://stackoverflow.com/a/929418/4678631
        public static IEnumerable<string> GetFiles(string path, Action<Exception> onError)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (var subDir in Directory.GetDirectories(path))
                        queue.Enqueue(subDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                    for (var i = 0; i < files.Length; i++)
                        yield return files[i];
            }
        }
    }
}
