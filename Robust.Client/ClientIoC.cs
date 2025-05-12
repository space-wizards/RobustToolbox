using System;
using Robust.Client.Audio;
using Robust.Client.Audio.Midi;
using Robust.Client.Configuration;
using Robust.Client.Console;
using Robust.Client.Debugging;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Client.HWId;
using Robust.Client.Input;
using Robust.Client.Localization;
using Robust.Client.Map;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.Profiling;
using Robust.Client.Prototypes;
using Robust.Client.Reflection;
using Robust.Client.Replays;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.ResourceManagement;
using Robust.Client.Serialization;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.Upload;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.Themes;
using Robust.Client.UserInterface.XAML.Proxy;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client
{
    internal static class ClientIoC
    {
        public static void RegisterIoC(GameController.DisplayMode mode, IDependencyCollection deps)
        {
            SharedIoC.RegisterIoC(deps);

            deps.Register<IGameTiming, ClientGameTiming>();
            deps.Register<IClientGameTiming, ClientGameTiming>();
            deps.Register<IPrototypeManager, ClientPrototypeManager>();
            deps.Register<IPrototypeManagerInternal, ClientPrototypeManager>();
            deps.Register<IMapManager, NetworkedMapManager>();
            deps.Register<IMapManagerInternal, NetworkedMapManager>();
            deps.Register<INetworkedMapManager, NetworkedMapManager>();
            deps.Register<IEntityManager, ClientEntityManager>();
            deps.Register<IReflectionManager, ClientReflectionManager>();
            deps.Register<IConsoleHost, ClientConsoleHost>();
            deps.Register<IClientConsoleHost, ClientConsoleHost>();
            deps.Register<IComponentFactory, ComponentFactory>();
            deps.Register<ITileDefinitionManager, ClydeTileDefinitionManager>();
            deps.Register<IClydeTileDefinitionManager, ClydeTileDefinitionManager>();
            deps.Register<ClydeTileDefinitionManager, ClydeTileDefinitionManager>();
            deps.Register<GameController, GameController>();
            deps.Register<IGameController, GameController>();
            deps.Register<IGameControllerInternal, GameController>();
            deps.Register<IResourceManager, ResourceCache>();
            deps.Register<IResourceManagerInternal, ResourceCache>();
            deps.Register<IResourceCache, ResourceCache>();
            deps.Register<IResourceCacheInternal, ResourceCache>();
            deps.Register<IClientNetManager, NetManager>();
            deps.Register<EntityManager, ClientEntityManager>();
            deps.Register<ClientEntityManager>();
            deps.Register<IClientEntityManager, ClientEntityManager>();
            deps.Register<IClientEntityManagerInternal, ClientEntityManager>();
            deps.Register<IEntityNetworkManager, ClientEntityManager>();
            deps.Register<IReplayLoadManager, ReplayLoadManager>();
            deps.Register<IReplayPlaybackManager, ReplayPlaybackManager>();
            deps.Register<IReplayRecordingManager, ReplayRecordingManager>();
            deps.Register<IReplayRecordingManagerInternal, ReplayRecordingManager>();
            deps.Register<IClientGameStateManager, ClientGameStateManager>();
            deps.Register<IBaseClient, BaseClient>();
            deps.Register<IPlayerManager, PlayerManager>();
            deps.Register<ISharedPlayerManager, PlayerManager>();
            deps.Register<IStateManager, StateManager>();
            deps.Register<IUserInterfaceManager, UserInterfaceManager>();
            deps.Register<IUserInterfaceManagerInternal, UserInterfaceManager>();
            deps.Register<ILightManager, LightManager>();
            deps.Register<IDiscordRichPresence, DiscordRichPresence>();
            deps.Register<IMidiManager, MidiManager>();
            deps.Register<IAuthManager, AuthManager>();
            deps.Register<ProfViewManager>();
            deps.Register<IGamePrototypeLoadManager, GamePrototypeLoadManager>();
            deps.Register<NetworkResourceManager>();
            deps.Register<IReloadManager, ReloadManager>();
            deps.Register<ILocalizationManager, ClientLocalizationManager>();
            deps.Register<ILocalizationManagerInternal, ClientLocalizationManager>();

            switch (mode)
            {
                case GameController.DisplayMode.Headless:
                    deps.Register<IClyde, ClydeHeadless>();
                    deps.Register<IClipboardManager, ClydeHeadless>();
                    deps.Register<IClydeInternal, ClydeHeadless>();
                    deps.Register<IAudioManager, HeadlessAudioManager>();
                    deps.Register<IAudioInternal, HeadlessAudioManager>();
                    deps.Register<IInputManager, InputManager>();
                    deps.Register<IFileDialogManager, DummyFileDialogManager>();
                    deps.Register<IUriOpener, UriOpenerDummy>();
                    break;
                case GameController.DisplayMode.Clyde:
                    deps.Register<IClyde, Clyde>();
                    deps.Register<IClipboardManager, Clyde>();
                    deps.Register<IClydeInternal, Clyde>();
                    deps.Register<IAudioManager, AudioManager>();
                    deps.Register<IAudioInternal, AudioManager>();
                    deps.Register<IInputManager, ClydeInputManager>();
                    deps.Register<IFileDialogManager, FileDialogManager>();
                    deps.Register<IUriOpener, UriOpener>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            deps.Register<IFontManager, FontManager>();
            deps.Register<IFontManagerInternal, FontManager>();
            deps.Register<IEyeManager, EyeManager>();
            deps.Register<IPlacementManager, PlacementManager>();
            deps.Register<IOverlayManager, OverlayManager>();
            deps.Register<IOverlayManagerInternal, OverlayManager>();
            deps.Register<IViewVariablesManager, ClientViewVariablesManager>();
            deps.Register<IClientViewVariablesManager, ClientViewVariablesManager>();
            deps.Register<IClientViewVariablesManagerInternal, ClientViewVariablesManager>();
            deps.Register<IClientConGroupController, ClientConGroupController>();
            deps.Register<IScriptClient, ScriptClient>();
            deps.Register<IRobustSerializer, ClientRobustSerializer>();
            deps.Register<IRobustSerializerInternal, ClientRobustSerializer>();
            deps.Register<IClientRobustSerializer, ClientRobustSerializer>();
            deps.Register<IConfigurationManager, ClientNetConfigurationManager>();
            deps.Register<INetConfigurationManager, ClientNetConfigurationManager>();
            deps.Register<IConfigurationManagerInternal, ClientNetConfigurationManager>();
            deps.Register<IClientNetConfigurationManager, ClientNetConfigurationManager>();
            deps.Register<INetConfigurationManagerInternal, ClientNetConfigurationManager>();

#if TOOLS
            deps.Register<IXamlProxyManager, XamlProxyManager>();
            deps.Register<IXamlHotReloadManager, XamlHotReloadManager>();
#else
            deps.Register<IXamlProxyManager, XamlProxyManagerStub>();
            deps.Register<IXamlHotReloadManager, XamlHotReloadManagerStub>();
#endif

            deps.Register<IXamlProxyHelper, XamlProxyHelper>();
            deps.Register<MarkupTagManager>();
            deps.Register<IHWId, BasicHWId>();
        }
    }
}
