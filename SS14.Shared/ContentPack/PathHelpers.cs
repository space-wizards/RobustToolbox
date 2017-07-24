using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SS14.Shared.ContentPack
{
    public static class PathHelpers
    {
        /// <summary>
        /// Get the full directory path that the executable is located in.
        /// </summary>
        public static string GetExecutableDirectory()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            string path = new Uri(assembly.CodeBase).LocalPath;
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Get the full path to a file relative to the executable containing directory.
        /// </summary>
        public static string ExecutableRelativeFile(string file)
        {
            return Path.GetFullPath(Path.Combine(GetExecutableDirectory(), file));
        }

        public static string AssemblyRelativeFile(string file, Assembly assembly)
        {
            string path = new Uri(assembly.CodeBase).LocalPath;
            return Path.Combine(GetExecutableDirectory(), file);
        }

        public static IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, (Exception e) => Console.WriteLine(e));
        }

        // Source: http://stackoverflow.com/a/929418/4678631
        public static IEnumerable<string> GetFiles(string path, Action<Exception> onError)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
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
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
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
