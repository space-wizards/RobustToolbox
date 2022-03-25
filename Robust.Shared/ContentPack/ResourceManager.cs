using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    internal partial class ResourceManager : IResourceManagerInternal
    {
        [Dependency] private readonly IConfigurationManager _config = default!;

        private readonly ReaderWriterLockSlim _contentRootsLock = new(LockRecursionPolicy.SupportsRecursion);

        private readonly List<(ResourcePath prefix, IContentRoot root)> _contentRoots =
            new();

        private StreamSeekMode _streamSeekMode;

        // Special file names on Windows like serial ports.
        private static readonly Regex BadPathSegmentRegex =
            new("^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase);

        // Literally not characters that can't go into filenames on Windows.
        private static readonly Regex BadPathCharacterRegex =
            new("[<>:\"|?*\0\\x01-\\x1f]", RegexOptions.IgnoreCase);

        /// <inheritdoc />
        public IWritableDirProvider UserData { get; private set; } = default!;

        public event Action<IContentRoot>? RootAdded;

        /// <inheritdoc />
        public void Initialize(string? userData)
        {
            if (userData != null)
            {
                UserData = new WritableDirProvider(Directory.CreateDirectory(userData));
            }
            else
            {
                UserData = new VirtualWritableDirProvider();
            }

            _config.OnValueChanged(CVars.ResStreamSeekMode, i => _streamSeekMode = (StreamSeekMode)i, true);
        }

        /// <inheritdoc />
        public void MountDefaultContentPack()
        {
            //Assert server only

            var zipPath = _config.GetCVar<string>("resource.pack");

            // no pack in config
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                Logger.WarningS("res", "No default ContentPack to load in configuration.");
                return;
            }

            MountContentPack(zipPath);
        }

        /// <inheritdoc />
        public void MountContentPack(string pack, ResourcePath? prefix = null)
        {
            prefix = SanitizePrefix(prefix);

            if (!Path.IsPathRooted(pack))
                pack = PathHelpers.ExecutableRelativeFile(pack);

            var packInfo = new FileInfo(pack);

            if (!packInfo.Exists)
            {
                throw new FileNotFoundException("Specified ContentPack does not exist: " + packInfo.FullName);
            }

            //create new PackLoader

            var loader = new PackLoader(packInfo);
            AddRoot(prefix, loader);
        }

        public void MountContentPack(Stream zipStream, ResourcePath? prefix = null)
        {
            prefix = SanitizePrefix(prefix);

            var loader = new PackLoader(zipStream);
            AddRoot(prefix, loader);
        }

        public void AddRoot(ResourcePath prefix, IContentRoot loader)
        {
            loader.Mount();
            _contentRootsLock.EnterWriteLock();
            try
            {
                _contentRoots.Add((prefix, loader));
            }
            finally
            {
                _contentRootsLock.ExitWriteLock();
            }

            RootAdded?.Invoke(loader);
        }

        private static ResourcePath SanitizePrefix(ResourcePath? prefix)
        {
            if (prefix == null)
            {
                prefix = ResourcePath.Root;
            }
            else if (!prefix.IsRooted)
            {
                throw new ArgumentException("Prefix must be rooted.", nameof(prefix));
            }

            return prefix;
        }

        /// <inheritdoc />
        public void MountContentDirectory(string path, ResourcePath? prefix = null)
        {
            prefix = SanitizePrefix(prefix);

            if (!Path.IsPathRooted(path))
                path = PathHelpers.ExecutableRelativeFile(path);

            var pathInfo = new DirectoryInfo(path);
            if (!pathInfo.Exists)
            {
                throw new DirectoryNotFoundException("Specified directory does not exist: " + pathInfo.FullName);
            }

            var loader = new DirLoader(pathInfo, Logger.GetSawmill("res"), _config.GetCVar(CVars.ResCheckPathCasing));
            AddRoot(prefix, loader);
        }

        /// <inheritdoc />
        public Stream ContentFileRead(string path)
        {
            return ContentFileRead(new ResourcePath(path));
        }

        /// <inheritdoc />
        public Stream ContentFileRead(ResourcePath path)
        {
            if (TryContentFileRead(path, out var fileStream))
            {
                return fileStream;
            }

            throw new FileNotFoundException($"Path does not exist in the VFS: '{path}'");
        }

        /// <inheritdoc />
        public bool TryContentFileRead(string path, [NotNullWhen(true)] out Stream? fileStream)
        {
            return TryContentFileRead(new ResourcePath(path), out fileStream);
        }

        /// <inheritdoc />
        public bool TryContentFileRead(ResourcePath path, [NotNullWhen(true)] out Stream? fileStream)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }
#if DEBUG
            if (!IsPathValid(path))
            {
                throw new FileNotFoundException($"Path '{path}' contains invalid characters/filenames.");
            }
#endif
            _contentRootsLock.EnterReadLock();

            try
            {
                foreach (var (prefix, root) in _contentRoots)
                {
                    if (!path.TryRelativeTo(prefix, out var relative))
                    {
                        continue;
                    }

                    if (root.TryGetFile(relative, out var stream))
                    {
                        fileStream = WrapStream(stream);
                        return true;
                    }
                }

                fileStream = null;
                return false;
            }
            finally
            {
                _contentRootsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Apply <see cref="_streamSeekMode"/> to the provided stream.
        /// </summary>
        private Stream WrapStream(Stream stream)
        {
            switch (_streamSeekMode)
            {
                case StreamSeekMode.None:
                    return stream;

                case StreamSeekMode.ForceSeekable:
                    if (stream.CanSeek)
                        return stream;

                    var ms = new MemoryStream(stream.CopyToArray(), writable: false);
                    stream.Dispose();
                    return ms;

                case StreamSeekMode.ForceNonSeekable:
                    if (!stream.CanSeek)
                        return stream;

                    return new NonSeekableStream(stream);

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <inheritdoc />
        public bool ContentFileExists(string path)
        {
            return ContentFileExists(new ResourcePath(path));
        }

        /// <inheritdoc />
        public bool ContentFileExists(ResourcePath path)
        {
            if (TryContentFileRead(path, out var stream))
            {
                stream.Dispose();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<ResourcePath> ContentFindFiles(string path)
        {
            return ContentFindFiles(new ResourcePath(path));
        }

        /// <inheritdoc />
        public IEnumerable<ResourcePath> ContentFindFiles(ResourcePath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.IsRooted)
            {
                throw new ArgumentException("Path is not rooted", nameof(path));
            }

            var alreadyReturnedFiles = new HashSet<ResourcePath>();

            _contentRootsLock.EnterReadLock();
            try
            {
                foreach (var (prefix, root) in _contentRoots)
                {
                    if (!path.TryRelativeTo(prefix, out var relative))
                    {
                        continue;
                    }

                    foreach (var filename in root.FindFiles(relative))
                    {
                        var newPath = prefix / filename;
                        if (!alreadyReturnedFiles.Contains(newPath))
                        {
                            alreadyReturnedFiles.Add(newPath);
                            yield return newPath;
                        }
                    }
                }
            }
            finally
            {
                _contentRootsLock.ExitReadLock();
            }
        }

        public bool TryGetDiskFilePath(ResourcePath path, [NotNullWhen(true)] out string? diskPath)
        {
            // loop over each root trying to get the file
            _contentRootsLock.EnterReadLock();
            try
            {
                foreach (var (prefix, root) in _contentRoots)
                {
                    if (root is not DirLoader dirLoader || !path.TryRelativeTo(prefix, out var tempPath))
                    {
                        continue;
                    }

                    diskPath = dirLoader.GetPath(tempPath);
                    if (File.Exists(diskPath))
                        return true;
                }

                diskPath = null;
                return false;
            }
            finally
            {
                _contentRootsLock.ExitReadLock();
            }
        }

        public void MountStreamAt(MemoryStream stream, ResourcePath path)
        {
            var loader = new SingleStreamLoader(stream, path.ToRelativePath());
            AddRoot(ResourcePath.Root, loader);
        }

        public IEnumerable<ResourcePath> GetContentRoots()
        {
            foreach (var (_, root) in _contentRoots)
            {
                if (root is DirLoader loader)
                {
                    yield return new ResourcePath(loader.GetPath(new ResourcePath(@"/")));
                }
            }
        }

        internal static bool IsPathValid(ResourcePath path)
        {
            var asString = path.ToString();
            if (BadPathCharacterRegex.IsMatch(asString))
            {
                return false;
            }

            foreach (var segment in path.EnumerateSegments())
            {
                if (BadPathSegmentRegex.IsMatch(segment))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
