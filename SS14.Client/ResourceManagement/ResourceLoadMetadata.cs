using SS14.Shared.Utility;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Basically contains everything the resource cache is capable of collecting about a resource being loaded,
    ///     to pass off to the resource loader itself.
    /// </summary>
    public abstract class ResourceLoadMetadata
    {
        /// <summary>
        ///     The full virtual path, basically what was passed to the method causing this resource load.
        /// </summary>
        public ResourcePath FullVirtualPath { get; }

        /// <summary>
        ///     Tries to get a <see cref="Stream"/> for the contents of the file.
        ///     This may not be available.
        /// </summary>
        /// <param name="stream">The memory stream, if any.</param>
        /// <returns>True if a file stream is available, false otherwise.</returns>
        public abstract bool TryGetFileStream(out Stream stream);

        /// <summary>
        ///     Tries to get a disk path for the file/directory.
        ///     This may not be available.
        /// </summary>
        /// <param name="diskPath">The disk path, if any.</param>
        /// <returns>True if a disk path is available, false otherwise.</returns>
        public abstract bool GetDiskPath(out string diskPath);
    }

    public partial class ResourceCache
    {
        private class ResourceLoadMetadataImpl : ResourceLoadMetadata
        {
            /// <summary>
            ///     The content root which claimed to have the file.
            /// </summary>
            public readonly IContentRoot ContentRoot;
            /// <summary>
            ///     The resource path relative to the content root.
            /// </summary>
            public readonly ResourcePath RootRelativePath;

            public override bool GetDiskPath(out string diskPath)
            {
                return ContentRoot.TryGetDiskPath(RootRelativePath, out diskPath);
            }

            public override bool TryGetFileStream(out Stream stream)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
