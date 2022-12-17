using System;
using Robust.Client.Audio.Midi;
using Robust.Client.Console;
using Robust.Client.Debugging;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Audio;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Input;
using Robust.Client.Map;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.Profiling;
using Robust.Client.Prototypes;
using Robust.Client.Reflection;
using Robust.Client.Replays;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Themes;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
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
            deps.Register<IMapManager, NetworkedMapManager>();
            deps.Register<IMapManagerInternal, NetworkedMapManager>();
            deps.Register<INetworkedMapManager, NetworkedMapManager>();
            deps.Register<IEntityManager, ClientEntityManager>();
            deps.Register<IReflectionManager, ClientReflectionManager>();
            deps.Register<IConsoleHost, ClientConsoleHost>();
            deps.Register<IClientConsoleHost, ClientConsoleHost>();
            deps.Register<IComponentFactory, ClientComponentFactory>();
            deps.Register<ITileDefinitionManager, ClydeTileDefinitionManager>();
            deps.Register<IClydeTileDefinitionManager, ClydeTileDefinitionManager>();
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
            deps.Register<IReplayRecordingManager, ReplayRecordingManager>();
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
            deps.Register<IPhysicsManager, PhysicsManager>();
            switch (mode)
            {
                case GameController.DisplayMode.Headless:
                    deps.Register<IClyde, ClydeHeadless>();
                    deps.Register<IClipboardManager, ClydeHeadless>();
                    deps.Register<IClydeInternal, ClydeHeadless>();
                    deps.Register<IClydeAudio, ClydeAudioHeadless>();
                    deps.Register<IClydeAudioInternal, ClydeAudioHeadless>();
                    deps.Register<IInputManager, InputManager>();
                    deps.Register<IFileDialogManager, DummyFileDialogManager>();
                    deps.Register<IUriOpener, UriOpenerDummy>();
                    break;
                case GameController.DisplayMode.Clyde:
                    deps.Register<IClyde, Clyde>();
                    deps.Register<IClipboardManager, Clyde>();
                    deps.Register<IClydeInternal, Clyde>();
                    deps.Register<IClydeAudio, FallbackProxyClydeAudio>();
                    deps.Register<IClydeAudioInternal, FallbackProxyClydeAudio>();
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
        }
    }
}
