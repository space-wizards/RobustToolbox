using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Client.ResourceManagement;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Utility;

namespace Robust.Client.Interfaces.ResourceManagement
{
    public interface IResourceCache : IResourceManager
    {
        T GetResource<T>(string path, bool useFallback = true)
            where T : BaseResource, new();

        T GetResource<T>(ResourcePath path, bool useFallback = true)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource)
            where T : BaseResource, new();

        bool TryGetResource<T>(ResourcePath path, [NotNullWhen(true)] out T? resource)
            where T : BaseResource, new();

        void ReloadResource<T>(string path)
            where T : BaseResource, new();

        void ReloadResource<T>(ResourcePath path)
            where T : BaseResource, new();

        void CacheResource<T>(string path, T resource)
            where T : BaseResource, new();

        void CacheResource<T>(ResourcePath path, T resource)
            where T : BaseResource, new();

        T GetFallback<T>()
            where T : BaseResource, new();

        IEnumerable<KeyValuePair<ResourcePath, T>> GetAllResources<T>() where T : BaseResource, new();

        // Resource load callbacks so content can hook stuff like click maps.
        event Action<TextureLoadedEventArgs> OnRawTextureLoaded;
        event Action<RsiLoadedEventArgs> OnRsiLoaded;

        /// <summary>
        ///     Loads the default content pack from the configuration file into the VFS.
        /// </summary>
        void MountDefaultContentPack();

        /// <summary>
        ///     Loads a content pack from disk into the VFS. The path is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="pack">The path of the pack to load on disk.</param>
        /// <param name="prefix">The resource path to which all files in the pack will be relative to in the VFS.</param>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="pack"/> does not exist on disk.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="pack"/> is null.</exception>
        void MountContentPack(string pack, ResourcePath? prefix = null);

        void MountContentPack(Stream zipStream, ResourcePath? prefix = null);

        /// <summary>
        ///     Adds a directory to search inside of to the VFS. The directory is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="path">The path of the directory to add to the VFS on disk.</param>
        /// <param name="prefix">The resource path to which all files in the directory will be relative to in the VFS.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="path"/> does not exist on disk.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> passed is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        void MountContentDirectory(string path, ResourcePath? prefix = null);

        /// <summary>
        ///     TODO: TEMPORARY: We need this because Godot can't load most resources without the disk easily.
        ///     Actually, seems like JetBrains Rider has trouble loading PBD files passed into AppDomain.Load too.
        ///     Hrm.
        /// </summary>
        bool TryGetDiskFilePath(ResourcePath path, [NotNullWhen(true)] out string? diskPath);
    }

    internal interface IResourceCacheInternal : IResourceCache, IResourceManagerInternal
    {
        void TextureLoaded(TextureLoadedEventArgs eventArgs);
        void RsiLoaded(RsiLoadedEventArgs eventArgs);
    }
}
