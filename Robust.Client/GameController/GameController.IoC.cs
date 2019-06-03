using Robust.Client.Console;
using Robust.Client.Debugging;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics.Lighting;
using Robust.Client.Input;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Map;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Map;
using Robust.Client.Player;
using Robust.Client.Reflection;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Timers;
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Placement;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Utility;
using Robust.Client.Utility;
using Robust.Client.Graphics.Overlays;
using Robust.Client.ViewVariables;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Robust.Client
{
    // Partial of GameController to initialize IoC and some other low-level systems like it.
    internal sealed partial class GameController
    {
        private static void InitIoC(DisplayMode mode)
        {
            RegisterIoC(mode);
            IoCManager.BuildGraph();
            RegisterReflection();
        }

        internal static void RegisterIoC(DisplayMode mode)
        {
            // Shared stuff.
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<IRobustSerializer, RobustSerializer>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<IEntityManager, ClientEntityManager>();
            IoCManager.Register<IComponentFactory, ClientComponentFactory>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<ITileDefinitionManager, ClydeTileDefinitionManager>();
            IoCManager.Register<IClydeTileDefinitionManager, ClydeTileDefinitionManager>();
            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IPhysicsManager, PhysicsManager>();
            IoCManager.Register<ITimerManager, TimerManager>();
            IoCManager.Register<ITaskManager, TaskManager>();
            IoCManager.Register<IRuntimeLog, RuntimeLog>();
            IoCManager.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            IoCManager.Register<ILocalizationManager, LocalizationManager>();

            // Client stuff.
            IoCManager.Register<IGameController, GameController>();
            IoCManager.Register<IGameControllerInternal, GameController>();
            IoCManager.Register<IReflectionManager, ClientReflectionManager>();
            IoCManager.Register<IResourceManager, ResourceCache>();
            IoCManager.Register<IResourceManagerInternal, ResourceCache>();
            IoCManager.Register<IResourceCache, ResourceCache>();
            IoCManager.Register<IResourceCacheInternal, ResourceCache>();
            IoCManager.Register<IClientNetManager, NetManager>();
            IoCManager.Register<IClientEntityManager, ClientEntityManager>();
            IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
            IoCManager.Register<IClientGameStateManager, ClientGameStateManager>();
            IoCManager.Register<IBaseClient, BaseClient>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IStateManager, StateManager>();
            IoCManager.Register<IUserInterfaceManager, UserInterfaceManager>();
            IoCManager.Register<IUserInterfaceManagerInternal, UserInterfaceManager>();
            IoCManager.Register<IDebugDrawing, DebugDrawing>();
            IoCManager.Register<ILightManager, LightManager>();
            IoCManager.Register<IDiscordRichPresence, DiscordRichPresence>();
            IoCManager.Register<IClientConsole, ClientConsole>();
            IoCManager.Register<IFontManager, FontManager>();
            IoCManager.Register<IFontManagerInternal, FontManager>();
            switch (mode)
            {
                case DisplayMode.Headless:
                    IoCManager.Register<IDisplayManager, ClydeHeadless>();
                    IoCManager.Register<IClyde, ClydeHeadless>();
                    IoCManager.Register<IInputManager, InputManager>();
                    break;
                case DisplayMode.Clyde:
                    IoCManager.Register<IDisplayManager, Clyde>();
                    IoCManager.Register<IClyde, Clyde>();
                    IoCManager.Register<IInputManager, ClydeInputManager>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            IoCManager.Register<IEyeManager, EyeManager>();
            IoCManager.Register<IGameTiming, GameTiming>();

            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<IOverlayManager, OverlayManager>();
            IoCManager.Register<IOverlayManagerInternal, OverlayManager>();
            IoCManager.Register<IViewVariablesManager, ViewVariablesManager>();
            IoCManager.Register<IViewVariablesManagerInternal, ViewVariablesManager>();

#if LINUX
            IoCManager.Register<IClipboardManager, ClipboardManagerLinux>();
#elif WINDOWS
            IoCManager.Register<IClipboardManager, ClipboardManagerWindows>();
#else
            IoCManager.Register<IClipboardManager, ClipboardManagerUnsupported>();
#endif

            IoCManager.Register<IModLoader, ModLoader>();
        }

        internal static void RegisterReflection()
        {
            // Gets a handle to the shared and the current (client) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                // Do NOT register Robust.Client.Godot.
                // At least not for now.
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
