using Robust.Shared;
using Robust.Shared.Utility;

namespace Robust.Client
{
    public class GameControllerOptions
    {
        /// <summary>
        ///     Whether content sandboxing will be enabled & enforced.
        /// </summary>
        public bool Sandboxing { get; init; } = true;

        // TODO: Expose mounting methods to games using Robust as a library.
        /// <summary>
        ///     Lists of mount options to mount.
        /// </summary>
        public MountOptions MountOptions { get; init; } = new();

        /// <summary>
        ///     Name the userdata directory will have.
        /// </summary>
        public string UserDataDirectoryName { get; init; } = "Space Station 14";

        /// <summary>
        ///     Name of the configuration file in the user data directory.
        /// </summary>
        public string ConfigFileName { get; init; } = "client_config.toml";

        // TODO: Define engine branding from json file in resources.
        /// <summary>
        ///     Default window title.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>RobustToolbox</c> if unset.
        /// </remarks>
        public string? DefaultWindowTitle { get; init; }

        /// <summary>
        ///     Assemblies with this prefix will be loaded.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>Content.</c> if unset.
        /// </remarks>
        public string? ContentModulePrefix { get; init; }

        /// <summary>
        ///     Name of the content build directory, for game pack mounting purposes.
        /// </summary>
        public string ContentBuildDirectory { get; init; } = "Content.Client";

        /// <summary>
        ///     Directory to load all assemblies from.
        /// </summary>
        public ResourcePath AssemblyDirectory { get; init; } = new(@"/Assemblies/");

        /// <summary>
        ///     Directory to load all prototypes from.
        /// </summary>
        public ResourcePath PrototypeDirectory { get; init; } = new(@"/Prototypes/");

        /// <summary>
        /// Directory resource path containing window icons to load.
        /// </summary>
        public ResourcePath? WindowIconSet { get; init; }

        /// <summary>
        /// Resource path for splash image to show when the game starts up.
        /// </summary>
        public ResourcePath? SplashLogo { get; init; }

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

        /// <summary>
        ///     Whether to disable command line args server auto-connecting.
        /// </summary>
        public bool DisableCommandLineConnect { get; init; } = false;
    }
}
