using System.Collections.Generic;
using System.IO;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.Utility;

namespace SS14.Shared.ContentPack
{
    /// <inheritdoc />
    internal class WritableDirProvider : IWritableDirProvider
    {
        private readonly string _rootDirString;

        /// <summary>
        /// Constructs an instance of <see cref="WritableDirProvider"/>.
        /// </summary>
        /// <param name="rootDir">Root file system directory to allow writing.</param>
        public WritableDirProvider(DirectoryInfo rootDir)
        {
            // FullName does not have a trailing separator, and we MUST have a separator.
            _rootDirString = rootDir.FullName + Path.DirectorySeparatorChar.ToString();
        }

        #region File Access

        /// <inheritdoc />
        public void Append(ResourcePath path, string content)
        {
            var fullPath = GetFullPath(path);
            File.AppendAllText(fullPath, content);
        }

        /// <inheritdoc />
        public void CreateDir(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            Directory.CreateDirectory(fullPath);
        }

        /// <inheritdoc />
        public void Delete(ResourcePath path)
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
        public bool Exists(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            return Directory.Exists(fullPath) || File.Exists(fullPath);
        }

        /// <inheritdoc />
        public (IEnumerable<ResourcePath> files, IEnumerable<ResourcePath> directories) Find(string pattern, bool recursive = true)
        {
            var rootLen = _rootDirString.Length - 1;
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(_rootDirString, pattern, option);
            var dirs = Directory.GetDirectories(_rootDirString, pattern, option);

            var resFiles = new List<ResourcePath>(files.Length);
            var resDirs = new List<ResourcePath>(dirs.Length);

            foreach (var file in files)
            {
                resFiles.Add(new ResourcePath(file.Substring(rootLen), "\\"));
            }

            foreach (var dir in dirs)
            {
                resDirs.Add(new ResourcePath(dir.Substring(rootLen), "\\"));
            }

            return (resFiles, resDirs);
        }

        /// <inheritdoc />
        public bool IsDir(ResourcePath path)
        {
            return Directory.Exists(GetFullPath(path));
        }

        /// <inheritdoc />
        public Stream Open(ResourcePath path, FileMode fileMode)
        {
            var fullPath = GetFullPath(path);
            return File.Open(fullPath, fileMode);
        }

        /// <inheritdoc />
        public string Read(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            return File.ReadAllText(fullPath);
        }

        /// <inheritdoc />
        public void Rename(ResourcePath oldPath, ResourcePath newPath)
        {
            var fullOldPath = GetFullPath(oldPath);
            var fullNewPath = GetFullPath(newPath);
            File.Move(fullOldPath, fullNewPath);
        }

        /// <inheritdoc />
        public void Write(ResourcePath path, string content)
        {
            var fullPath = GetFullPath(path);
            File.WriteAllText(fullPath, content);
        }

        #endregion

        private string GetFullPath(ResourcePath path)
        {
            return GetFullPath(_rootDirString, path);
        }

        private static string GetFullPath(string root, ResourcePath path)
        {
            var relPath = path.Clean().ToRelativePath().ToString();
            return Path.GetFullPath(Path.Combine(root, relPath));
        }
    }
}
