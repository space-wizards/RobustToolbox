using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    [Virtual]
    internal partial class ResourceManager : IResourceManagerInternal
    {
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private (ResPath prefix, IContentRoot root)[] _contentRoots =
            new (ResPath prefix, IContentRoot root)[0];

        private StreamSeekMode _streamSeekMode;
        private readonly object _rootMutateLock = new();

        // Special file names on Windows like serial ports.
        private static readonly Regex BadPathSegmentRegex =
            new("^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase);

        // Literally not characters that can't go into filenames on Windows.
        private static readonly Regex BadPathCharacterRegex =
            new("[<>:\"|?*\0\\x01-\\x1f]", RegexOptions.IgnoreCase);

        protected ISawmill Sawmill = default!;

        /// <inheritdoc />
        public IWritableDirProvider UserData { get; private set; } = default!;

        /// <inheritdoc />
        public virtual void Initialize(string? userData)
        {
            Sawmill = _logManager.GetSawmill("res");

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
                Sawmill.Warning("No default ContentPack to load in configuration.");
                return;
            }

            MountContentPack(zipPath);
        }

        /// <inheritdoc />
        public void MountContentPack(string pack, ResPath? prefix = null)
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

            var loader = new PackLoader(packInfo, Sawmill);
            AddRoot(prefix.Value, loader);
        }

        public void MountContentPack(Stream zipStream, ResPath? prefix = null)
        {
            prefix = SanitizePrefix(prefix);

            var loader = new PackLoader(zipStream, Sawmill);
            AddRoot(prefix.Value, loader);
        }

        public void AddRoot(ResPath prefix, IContentRoot loader)
        {
            lock (_rootMutateLock)
            {
                loader.Mount();

                // When adding a new root we atomically swap it into the existing list.
                // So the list of content roots is thread safe.
                // This does make adding new roots O(n). Oh well.
                var copy = _contentRoots;
                Array.Resize(ref copy, copy.Length + 1);
                copy[^1] = (prefix, loader);
                _contentRoots = copy;
            }
        }

        private static ResPath SanitizePrefix(ResPath? prefix)
        {
            if (prefix == null)
            {
                prefix = ResPath.Root;
            }
            else if (!prefix.Value.IsRooted)
            {
                throw new ArgumentException("Prefix must be rooted.", nameof(prefix));
            }

            return prefix.Value;
        }

        /// <inheritdoc />
        public void MountContentDirectory(string path, ResPath? prefix = null)
        {
            prefix = SanitizePrefix(prefix);

            if (!Path.IsPathRooted(path))
                path = PathHelpers.ExecutableRelativeFile(path);

            var pathInfo = new DirectoryInfo(path);
            if (!pathInfo.Exists)
            {
                throw new DirectoryNotFoundException("Specified directory does not exist: " + pathInfo.FullName);
            }

            var loader = new DirLoader(pathInfo, _logManager.GetSawmill("res"), _config.GetCVar(CVars.ResCheckPathCasing));
            AddRoot(prefix.Value, loader);
        }

        /// <inheritdoc />
        public Stream ContentFileRead(string path)
        {
            return ContentFileRead(new ResPath(path));
        }

        /// <inheritdoc />
        public Stream ContentFileRead(ResPath path)
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
            return TryContentFileRead(new ResPath(path), out fileStream);
        }

        /// <inheritdoc />
        public bool TryContentFileRead(ResPath? path, [NotNullWhen(true)] out Stream? fileStream)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.Value.IsRooted)
            {
                throw new ArgumentException($"Path '{path}' must be rooted", nameof(path));
            }
#if DEBUG
            if (!IsPathValid(path.Value))
            {
                throw new FileNotFoundException($"Path '{path}' contains invalid characters/filenames.");
            }
#endif

            if (path.Value.CanonPath.EndsWith(ResPath.Separator))
            {
                // This is a folder, not a file.
                fileStream = null;
                return false;
            }

            foreach (var (prefix, root) in _contentRoots)
            {
                if (!path.Value.TryRelativeTo(prefix, out var relative))
                {
                    continue;
                }

                if (root.TryGetFile(relative.Value, out var stream))
                {
                    fileStream = WrapStream(stream);
                    return true;
                }
            }

            fileStream = null;
            return false;
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
            return ContentFileExists(new ResPath(path));
        }

        /// <inheritdoc />
        public bool ContentFileExists(ResPath path)
        {
            if (TryContentFileRead(path, out var stream))
            {
                stream.Dispose();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<ResPath> ContentFindFiles(string path)
        {
            return ContentFindFiles(new ResPath(path));
        }

        public IEnumerable<string> ContentGetDirectoryEntries(ResPath path)
        {
            if (!path.IsRooted)
                throw new ArgumentException("Path is not rooted", nameof(path));

            // If we don't do this, TryRelativeTo won't work correctly.
            if (!path.CanonPath.EndsWith("/"))
                path = new ResPath(path.CanonPath + "/");

            var entries = new HashSet<string>();

            foreach (var (prefix, root) in _contentRoots)
            {
                if (!path.TryRelativeTo(prefix, out var relative))
                {
                    continue;
                }

                entries.UnionWith(root.GetEntries(relative.Value));
            }

            // We have to add mount points too.
            // e.g. during development, /Assemblies/ is a mount point,
            // and there's no explicit /Assemblies/ folder in Resources.
            // So we need to manually add it since the previous pass won't catch it at all.
            foreach (var (prefix, _) in _contentRoots)
            {
                if (!prefix.TryRelativeTo(path, out var relative))
                    continue;

                // Return first relative segment, unless it's literally just "." (identical path).
                var segments = relative.Value.EnumerateSegments();
                if (segments is ["."])
                    continue;

                entries.Add(segments[0] + "/");
            }

            return entries;
        }

        /// <inheritdoc />
        public IEnumerable<ResPath> ContentFindFiles(ResPath? path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.Value.IsRooted)
            {
                throw new ArgumentException("Path is not rooted", nameof(path));
            }

            var alreadyReturnedFiles = new HashSet<ResPath>();

            foreach (var (prefix, root) in _contentRoots)
            {
                if (!path.Value.TryRelativeTo(prefix, out var relative))
                {
                    continue;
                }

                foreach (var filename in root.FindFiles(relative.Value))
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

        public bool TryGetDiskFilePath(ResPath path, [NotNullWhen(true)] out string? diskPath)
        {
            // loop over each root trying to get the file
            foreach (var (prefix, root) in _contentRoots)
            {
                if (root is not DirLoader dirLoader || !path.TryRelativeTo(prefix, out var tempPath))
                {
                    continue;
                }

                diskPath = dirLoader.GetPath(tempPath.Value);
                if (File.Exists(diskPath))
                    return true;
            }

            diskPath = null;
            return false;
        }

        public void MountStreamAt(MemoryStream stream, ResPath path)
        {
            var loader = new SingleStreamLoader(stream, path.ToRelativePath());
            AddRoot(ResPath.Root, loader);
        }

        public IEnumerable<ResPath> GetContentRoots()
        {
            foreach (var (_, root) in _contentRoots)
            {
                if (root is DirLoader loader)
                {
                    var rootDir = loader.GetPath(new ResPath(@"/"));

                    yield return new ResPath(rootDir);
                }
            }
        }

        internal static bool IsPathValid(ResPath path)
        {
            var asString = path.ToString();
            if (BadPathCharacterRegex.IsMatch(asString))
            {
                return false;
            }

            foreach (var segment in path.CanonPath.Split('/'))
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
