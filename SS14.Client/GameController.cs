using SS14.Client.GodotGlue;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    public sealed partial class GameController : ClientEntryPoint
    {
        public Godot.SceneTree SceneTree { get; private set; }

        [Dependency]
        readonly IConfigurationManager _configurationManager;
        [Dependency]
        readonly IResourceCache _resourceCache;
        /*
        [Dependency]
        readonly private INetworkGrapher _netGrapher;
        [Dependency]
        readonly private IClientNetManager _networkManager;
        [Dependency]
        readonly private IStateManager _stateManager;
        [Dependency]
        readonly private IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly private ITileDefinitionManager _tileDefinitionManager;
        [Dependency]
        readonly private ISS14Serializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resourceManager;
        [Dependency]
        private readonly IMapManager _mapManager;
        [Dependency]
        private readonly IPlacementManager _placementManager;
        */

        public override void Main(Godot.SceneTree tree)
        {
            SceneTree = tree;

            InitIoC();

            // Load config.
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            // Init resources.
            _resourceCache.LoadBaseResources();

            var texture = _resourceCache.GetResource<TextureResource>("Textures/Items/Toolbox_b.png");
            var s = new Godot.Sprite();
            Console.WriteLine("New sprite...");
            SceneTree.GetRoot().AddChild(s);
            Console.WriteLine("Added as child...");
            s.Texture = texture.Texture;
            Console.WriteLine("What the fuck?");
        }
    }
}
