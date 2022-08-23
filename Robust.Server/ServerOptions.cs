using Robust.Shared;
using Robust.Shared.Utility;

namespace Robust.Server
{
    public sealed class ServerOptions
    {
        /// <summary>
        ///     Whether content sandboxing will be enabled & enforced.
        /// </summary>
        public bool Sandboxing { get; init; } = false;

        // TODO: Expose mounting methods to games using Robust as a library.
        /// <summary>
        ///     Lists of mount options to mount.
        /// </summary>
        public MountOptions MountOptions { get; init; } = new();

        /// <summary>
        ///     Assemblies with this prefix will be loaded.
        /// </summary>
        public string ContentModulePrefix { get; init; } = "Content.";

        /// <summary>
        ///     Name of the content build directory, for game pack mounting purposes.
        /// </summary>
        public string ContentBuildDirectory { get; init; } = "Content.Server";

        /// <summary>
        ///     Directory to load all assemblies from.
        /// </summary>
        public ResourcePath AssemblyDirectory { get; init; } = new(@"/Assemblies/");

        /// <summary>
        ///     Directory to load all prototypes from.
        /// </summary>
        public ResourcePath PrototypeDirectory { get; init; } = new(@"/Prototypes/");

        /// <summary>
        ///     Whether to disable mounting the "Resources/" folder on FULL_RELEASE.
        /// </summary>
        public bool ResourceMountDisabled { get; init; } = false;

        /// <summary>
        ///     Whether to mount content resources when not on FULL_RELEASE.
        /// </summary>
        public bool LoadContentResources { get; init; } = true;

        /// <summary>
        ///     Whether to load config and user data.
        /// </summary>
        public bool LoadConfigAndUserData { get; init; } = true;
    }
}
