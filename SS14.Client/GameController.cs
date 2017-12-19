using SS14.Client.GodotGlue;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    public partial class GameController : ClientEntryPoint
    {
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

        public override void Main()
        {
            InitIoC();

            // Load config.
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            // Init resources.
            _resourceCache.LoadBaseResources();
        }
    }
}
