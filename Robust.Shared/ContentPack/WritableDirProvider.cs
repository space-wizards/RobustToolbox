using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <inheritdoc />
    internal sealed class WritableDirProvider : IWritableDirProvider
    {
        /// <inheritdoc />
        public string RootDir { get; }

        /// <summary>
        /// Constructs an instance of <see cref="WritableDirProvider"/>.
        /// </summary>
        /// <param name="rootDir">Root file system directory to allow writing.</param>
        public WritableDirProvider(DirectoryInfo rootDir)
        {
            // FullName does not have a trailing separator, and we MUST have a separator.
            RootDir = rootDir.FullName + Path.DirectorySeparatorChar.ToString();
        }

        #region File Access

        /// <inheritdoc />
        public void CreateDir(ResPath path)
        {
            var fullPath = GetFullPath(path);
            Directory.CreateDirectory(fullPath);
        }

        /// <inheritdoc />
        public void Delete(ResPath path)
        {
            var fullPath = GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
            else if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <inheritdoc />
        public bool Exists(ResPath path)
        {
            var fullPath = GetFullPath(path);
            return Directory.Exists(fullPath) || File.Exists(fullPath);
        }

        /// <inheritdoc />
        public (IEnumerable<ResPath> files, IEnumerable<ResPath> directories) Find(string pattern, bool recursive = true)
        {
            if (pattern.Contains(".."))
                throw new InvalidOperationException($"Pattern may not contain '..'. Pattern: {pattern}.");

            var rootLen = RootDir.Length - 1;
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(RootDir, pattern, option);
            var dirs = Directory.GetDirectories(RootDir, pattern, option);

            var resFiles = new List<ResPath>(files.Length);
            var resDirs = new List<ResPath>(dirs.Length);

            foreach (var file in files)
            {
                if (file.Contains("\\..") || file.Contains("/.."))
                    continue;

                resFiles.Add(ResPath.FromRelativeSystemPath(file.Substring(rootLen)).ToRootedPath());
            }

            foreach (var dir in dirs)
            {
                if (dir.Contains("\\..") || dir.Contains("/.."))
                    continue;

                resDirs.Add(ResPath.FromRelativeSystemPath(dir.Substring(rootLen)).ToRootedPath());
            }

            return (resFiles, resDirs);
        }

        public IEnumerable<string> DirectoryEntries(ResPath path)
        {
            var fullPath = GetFullPath(path);

            if (!Directory.Exists(fullPath))
                yield break;

            foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath))
            {
                yield return Path.GetRelativePath(fullPath, entry);
            }
        }

        /// <inheritdoc />
        public bool IsDir(ResPath path)
        {
            return Directory.Exists(GetFullPath(path));
        }

        /// <inheritdoc />
        public Stream Open(ResPath path, FileMode fileMode, FileAccess access, FileShare share)
        {
            var fullPath = GetFullPath(path);
            return File.Open(fullPath, fileMode, access, share);
        }

        public IWritableDirProvider OpenSubdirectory(ResPath path)
        {
            if (!IsDir(path))
                throw new FileNotFoundException();

            var dirInfo = new DirectoryInfo(GetFullPath(path));
            return new WritableDirProvider(dirInfo);
        }

        /// <inheritdoc />
        public void Rename(ResPath oldPath, ResPath newPath)
        {
            var fullOldPath = GetFullPath(oldPath);
            var fullNewPath = GetFullPath(newPath);
            File.Move(fullOldPath, fullNewPath);
        }

        public void OpenOsWindow(ResPath path)
        {
            if (!IsDir(path))
                path = path.Directory;

            var fullPath = GetFullPath(path);
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\explorer.exe",
                    Arguments = ".",
                    WorkingDirectory = fullPath,
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = ".",
                    WorkingDirectory = fullPath,
                });
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = ".",
                    WorkingDirectory = fullPath,
                });
            }
            else
            {
                throw new NotSupportedException("Opening OS windows not supported on this OS");
            }
        }

        #endregion

        public string GetFullPath(ResPath path)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException($"Path must be rooted. Path: {path}");
            }

            path = path.Clean();

            return GetFullPath(RootDir, path);
        }

        private static string GetFullPath(string root, ResPath path)
        {
            var relPath = path.ToRelativeSystemPath();
            if (relPath.Contains("\\..") || relPath.Contains("/.."))
            {
                // Hard cap on any exploit smuggling a .. in there.
                // Since that could allow leaving sandbox.
                throw new InvalidOperationException($"This branch should never be reached. Path: {path}");
            }

            return Path.GetFullPath(Path.Combine(root, relPath));
        }
    }
}
