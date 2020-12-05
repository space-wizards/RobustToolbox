using System;
using System.IO;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    public static class ResourceManagerExt
    {
        /// <summary>
        ///     Read a file from the mounted content roots, if it exists.
        /// </summary>
        /// <param name="res">The resource manager.</param>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file, or null if the file does not exist.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <seealso cref="IResourceManager.ContentFileRead(ResourcePath)"/>
        /// <seealso cref="IResourceManager.TryContentFileRead(ResourcePath, out Stream)"/>
        public static Stream? ContentFileReadOrNull(this IResourceManager res, ResourcePath path)
        {
            if (res.TryContentFileRead(path, out var stream))
            {
                return stream;
            }

            return null;
        }
    }
}
