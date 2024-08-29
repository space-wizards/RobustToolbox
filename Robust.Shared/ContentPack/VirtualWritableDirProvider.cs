using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    // TODO: Maybe provide consistency with System.IO on the front of which exception types get thrown.
    /// <summary>
    ///     Writable dir provider that uses an in-memory virtual file system, not the real OS file system.
    /// </summary>
    public sealed class VirtualWritableDirProvider : IWritableDirProvider
    {
        // Just a simple tree. No need to over complicate this.
        private readonly DirectoryNode _rootDirectoryNode;

        public VirtualWritableDirProvider()
        {
            _rootDirectoryNode = new();
        }

        private VirtualWritableDirProvider(DirectoryNode node)
        {
            _rootDirectoryNode = node;
        }

        /// <inheritdoc />
        public string? RootDir => null;

        public void CreateDir(ResPath path)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }

            path = path.Clean();

            var directory = _rootDirectoryNode;

            foreach (var segment in path.EnumerateSegments())
            {
                if (directory.Children.TryGetValue(segment, out var child))
                {
                    if (child is not DirectoryNode childDir)
                    {
                        throw new ArgumentException("A file already exists at that location.");
                    }

                    directory = childDir;
                    continue;
                }

                var newDir = new DirectoryNode();

                directory.Children.Add(segment, newDir);
                directory = newDir;
            }
        }

        public void Delete(ResPath path)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }

            path = path.Clean();

            var pathParent = path.Directory;

            if (!TryGetNodeAt(pathParent, out var parent) || !(parent is DirectoryNode directory))
            {
                return;
            }

            directory.Children.Remove(path.Filename);
        }

        public bool Exists(ResPath path)
        {
            return TryGetNodeAt(path, out _);
        }

        public (IEnumerable<ResPath> files, IEnumerable<ResPath> directories) Find(string pattern,
            bool recursive = true)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> DirectoryEntries(ResPath path)
        {
            if (!TryGetNodeAt(path, out var dir) || dir is not DirectoryNode dirNode)
                throw new ArgumentException("Path is not a valid directory node.");

            return dirNode.Children.Keys;
        }

        public bool IsDir(ResPath path)
        {
            return TryGetNodeAt(path, out var node) && node is DirectoryNode;
        }

        public Stream Open(ResPath path, FileMode fileMode, FileAccess access, FileShare share)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }

            var parentPath = path.Directory;
            if (!TryGetNodeAt(parentPath, out var parent) || !(parent is DirectoryNode parentDir))
            {
                throw new ArgumentException("Parent directory does not exist.");
            }

            var fileName = path.Filename;

            if (parentDir.Children.TryGetValue(fileName, out var maybeFileNode) && maybeFileNode is DirectoryNode)
            {
                throw new ArgumentException("There is a directory at that location.");
            }

            var fileNode = (FileNode) maybeFileNode!;

            switch (fileMode)
            {
                case FileMode.Append:
                {
                    if (fileNode == null)
                    {
                        fileNode = new FileNode();
                        parentDir.Children.Add(fileName, fileNode);
                    }

                    return new VirtualFileStream(fileNode.Contents, false, true, fileNode.Contents.Length);
                }

                case FileMode.Create:
                {
                    if (fileNode == null)
                    {
                        fileNode = new FileNode();
                        parentDir.Children.Add(fileName, fileNode);
                    }
                    else
                    {
                        // Clear contents if it already exists.
                        fileNode.Contents.SetLength(0);
                    }

                    return new VirtualFileStream(fileNode.Contents, true, true, 0);
                }

                case FileMode.CreateNew:
                {
                    if (fileNode != null)
                    {
                        throw new IOException("File already exists.");
                    }

                    fileNode = new FileNode();
                    parentDir.Children.Add(fileName, fileNode);

                    return new VirtualFileStream(fileNode.Contents, true, true, 0);
                }

                case FileMode.Open:
                {
                    if (fileNode == null)
                    {
                        throw new FileNotFoundException();
                    }

                    return new VirtualFileStream(fileNode.Contents, true, true, 0);
                }

                case FileMode.OpenOrCreate:
                {
                    if (fileNode == null)
                    {
                        fileNode = new FileNode();
                        parentDir.Children.Add(fileName, fileNode);
                    }

                    return new VirtualFileStream(fileNode.Contents, true, true, 0);
                }

                case FileMode.Truncate:
                {
                    if (fileNode == null)
                    {
                        throw new FileNotFoundException();
                    }

                    fileNode.Contents.SetLength(0);

                    return new VirtualFileStream(fileNode.Contents, true, true, 0);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(fileMode), fileMode, null);
            }
        }

        public void Rename(ResPath oldPath, ResPath newPath)
        {
            if (!oldPath.IsRooted)
                throw new ArgumentException("Path must be rooted", nameof(oldPath));

            if (!newPath.IsRooted)
                throw new ArgumentException("Path must be rooted", nameof(newPath));

            if (!TryGetNodeAt(oldPath.Directory, out var parent) || parent is not DirectoryNode sourceDir)
                throw new ArgumentException("Source directory does not exist.");

            if (!TryGetNodeAt(newPath.Directory, out var target) || target is not DirectoryNode targetDir)
                throw new ArgumentException("Target directory does not exist.");

            var newFile = newPath.Filename;
            if (targetDir.Children.ContainsKey(newFile))
                throw new ArgumentException("Target node already exists");

            var oldFile = oldPath.Filename;
            if (!sourceDir.Children.TryGetValue(oldFile, out var node))
                throw new ArgumentException("Node does not exist in original directory.");

            sourceDir.Children.Remove(oldFile);
            targetDir.Children.Add(newFile, node);
        }

        public void OpenOsWindow(ResPath path)
        {
            // Not valid for virtual directories. As this has no effect on the rest of the game no exception is thrown.
        }

        private bool TryGetNodeAt(ResPath path, [NotNullWhen(true)] out INode? node)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }

            path = path.Clean();

            if (path == ResPath.Root)
            {
                node = _rootDirectoryNode;
                return true;
            }

            var directory = _rootDirectoryNode;
            var segments = path.EnumerateSegments();
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (!directory.Children.TryGetValue(segment, out var child))
                {
                    node = default;
                    return false;
                }

                if (i == segments.Length - 1)
                {
                    node = child;
                    return true;
                }

                directory = (DirectoryNode) child;
            }

            throw new InvalidOperationException("Unreachable.");
        }

        public IWritableDirProvider OpenSubdirectory(ResPath path)
        {
            if (!TryGetNodeAt(path, out var node) || node is not DirectoryNode dirNode)
                throw new FileNotFoundException();

            return new VirtualWritableDirProvider(dirNode);
        }

        private interface INode
        {
        }

        private sealed class FileNode : INode
        {
            public MemoryStream Contents { get; } = new();
        }

        private sealed class DirectoryNode : INode
        {
            public Dictionary<string, INode> Children { get; } = new();
        }

        private sealed class VirtualFileStream : Stream
        {
            private readonly MemoryStream _source;

            public VirtualFileStream(MemoryStream source, bool canRead, bool canWrite, long initialPosition)
            {
                _source = source;
                CanRead = canRead;
                CanWrite = canWrite;
                Position = initialPosition;
            }

            public override void Flush()
            {
                // Nada.
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!CanRead)
                {
                    throw new InvalidOperationException("Cannot read from this stream.");
                }

                _source.Position = Position;
                var read = _source.Read(buffer, offset, count);
                Position = _source.Position;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;
                    case SeekOrigin.Current:
                        Position += offset;
                        break;
                    case SeekOrigin.End:
                        Position = Length + offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _source.Position = Position;
                _source.Write(buffer, offset, count);
                Position = _source.Position;
            }

            public override bool CanRead { get; }
            public override bool CanSeek => true;
            public override bool CanWrite { get; }
            public override long Length => _source.Position;
            public override long Position { get; set; }
        }
    }
}
