using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SS14.Shared.ContentPack;
using SS14.Shared.Utility;

namespace SS14.Client.Utility
{
    // Don't use this outside debug builds.
    // Seriously.
    // For release builds, this should be handled by the release packaging script.
#if DEBUG
    /// <summary>
    ///     Handles the automatic copying of resources that need to be accessible to Godot's res://.
    ///     That's .tscn files, font files and GUI image files.
    ///     I say copy but if you're on Unix, it'll use symlinks. Like a sane person.
    /// </summary>
    public static class GodotResourceCopy
    {
        /// <summary>
        ///     Ensure that the directory <paramref name="targetPath"/> is a mirror of <paramref name="sourcePath"/>.
        ///     On Windows this does a recursive copy.
        ///     On Unix it uses a symlink.
        /// </summary>
        public static void DoDirCopy(string sourcePath, string targetPath)
        {
#if UNIX
            if (Directory.Exists(targetPath))
            {
                return;
            }
            var process = Process.Start("ln", $"-s \"{sourcePath}\" \"{targetPath}\"");
            if (process == null)
            {
                throw new IOException();
            }

            process.WaitForExit();
            DebugTools.Assert(process.ExitCode == 0);
#else
            if (File.Exists(Path.Combine(targetPath, "I_MADE_THE_SYMLINK")))
            {
                return;
            }

            var dirQueue = new Queue<string>();
            dirQueue.Enqueue(".");

            while (dirQueue.Count > 0)
            {
                var relative = dirQueue.Dequeue();
                var sourceRelative = Path.Combine(sourcePath, relative);
                var targetRelative = Path.Combine(targetPath, relative);

                Directory.CreateDirectory(targetRelative);

                foreach (var dirChild in Directory.EnumerateDirectories(sourceRelative).Select(Path.GetFileName))
                {
                    dirQueue.Enqueue(Path.Combine(relative, dirChild));
                }

                foreach (var fileChild in Directory.EnumerateFiles(sourceRelative).Select(Path.GetFileName))
                {
                    var sourceFile = Path.Combine(sourceRelative, fileChild);
                    var targetFile = Path.Combine(targetRelative, fileChild);

                    if (!File.Exists(targetFile) || File.GetLastWriteTime(targetFile) < File.GetLastWriteTime(sourceFile))
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                }
            }
#endif
        }
    }
#endif // DEBUG
}
