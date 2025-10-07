using System;
using System.Threading;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : IClydeInternal, IPostInjectInit, IEntityEventSubscriber
    {
        [Dependency] private readonly IClydeTileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IResourceManager _resManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly ClientEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IReloadManager _reloads = default!;

        private bool _drawingSplash = true;

        private float _lightResolutionScale = 0.5f;
        private int _maxLights = 2048;
        private int _maxOccluders = 2048;
        private int _maxShadowcastingLights = 128;
        private bool _enableSoftShadows = true;

        private Thread? _gameThread;

        private ISawmill _sawmillWin = default!;
        private ISawmill _clydeSawmill = default!;

        private bool _threadWindowApi;

        public Clyde()
        {
            SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;
        }

        public bool InitializePreWindowing()
        {
            _clydeSawmill = _logManager.GetSawmill("clyde");
            _sawmillWin = _logManager.GetSawmill("clyde.win");

            _reloads.Register("/Shaders", "*.swsl");
            _reloads.Register("/Textures/Shaders", "*.swsl");
            _reloads.Register("/Textures", "*.jpg");
            _reloads.Register("/Textures", "*.jpeg");
            _reloads.Register("/Textures", "*.png");
            _reloads.Register("/Textures", "*.webp");

            _reloads.OnChanged += OnChange;
            _proto.PrototypesReloaded += OnProtoReload;


            _cfg.OnValueChanged(CVars.DisplayVSync, b => VsyncEnabled = b, true);
            _cfg.OnValueChanged(CVars.DisplayWindowMode, WindowModeChanged, true);
            /*
            _cfg.OnValueChanged(CVars.LightResolutionScale, LightResolutionScaleChanged, true);
            _cfg.OnValueChanged(CVars.MaxShadowcastingLights, MaxShadowcastingLightsChanged, true);
            _cfg.OnValueChanged(CVars.LightSoftShadows, SoftShadowsChanged, true);
            _cfg.OnValueChanged(CVars.MaxLightCount, MaxLightsChanged, true);
            _cfg.OnValueChanged(CVars.MaxOccluderCount, MaxOccludersChanged, true);
            _cfg.OnValueChanged(CVars.RenderTileEdges, RenderTileEdgesChanges, true);
            */
            // I can't be bothered to tear down and set these threads up in a cvar change handler.

            // Windows and Linux can be trusted to not explode with threaded windowing,
            // macOS cannot.
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                _cfg.OverrideDefault(CVars.DisplayThreadWindowApi, true);

            _threadWindowApi = _cfg.GetCVar(CVars.DisplayThreadWindowApi);

            InitKeys();

            return InitWindowing();
        }

        private void OnProtoReload(PrototypesReloadedEventArgs obj)
        {
            if (!obj.WasModified<ShaderPrototype>())
                return;

            foreach (var shader in obj.ByType[typeof(ShaderPrototype)].Modified.Keys)
            {
                _resourceCache.ReloadResource<ShaderSourceResource>(shader);
            }
        }

        private void OnChange(ResPath obj)
        {
            if ((obj.TryRelativeTo(new ResPath("/Shaders"), out _) || obj.TryRelativeTo(new ResPath("/Textures/Shaders"), out _)) && obj.Extension == "swsl")
            {
                _resourceCache.ReloadResource<ShaderSourceResource>(obj);
            }

            if (obj.TryRelativeTo(new ResPath("/Textures"), out _) && !obj.TryRelativeTo(new ResPath("/Textures/Tiles"), out _))
            {
                if (obj.Extension == "jpg" || obj.Extension == "jpeg" || obj.Extension == "webp")
                {
                    _resourceCache.ReloadResource<TextureResource>(obj);
                }

                if (obj.Extension == "png")
                {
                    _resourceCache.ReloadResource<TextureResource>(obj);
                }
            }
        }

        public bool InitializePostWindowing()
        {
            _gameThread = Thread.CurrentThread;

            InitSystems();

            if (!InitMainWindowAndRenderer())
                return false;

            return true;
        }

        public bool SeparateWindowThread => _threadWindowApi;

        public void EnterWindowLoop()
        {
            _windowing!.EnterWindowLoop();
        }

        public void TerminateWindowLoop()
        {
            _windowing!.TerminateWindowLoop();
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            if (!_threadWindowApi)
            {
                _windowing!.PollEvents();
            }

            _windowing?.FlushDispose();
            FlushShaderInstanceDispose();
            // FlushRenderTargetDispose();
            FlushTextureDispose();
            FlushViewportDispose();
        }

        public void Ready()
        {
            _drawingSplash = false;

            // InitLighting();
        }

        public IClydeDebugInfo DebugInfo => _debugInfo ?? throw new InvalidOperationException("Not initialized yet");
        public IClydeDebugStats DebugStats => _debugStats;

        public void PostInject()
        {
            // This cvar does not modify the actual GL version requested or anything,
            // it overrides the version we detect to detect GL features.
            // RegisterBlockCVars();
        }

        public void RegisterGridEcsEvents()
        {
            // _entityManager.EventBus.SubscribeEvent<TileChangedEvent>(EventSource.Local, this, _updateTileMapOnUpdate);
            // _entityManager.EventBus.SubscribeEvent<GridStartupEvent>(EventSource.Local, this, _updateOnGridCreated);
            // _entityManager.EventBus.SubscribeEvent<GridRemovalEvent>(EventSource.Local, this, _updateOnGridRemoved);
        }

        public void ShutdownGridEcsEvents()
        {
            _entityManager.EventBus.UnsubscribeEvent<TileChangedEvent>(EventSource.Local, this);
            _entityManager.EventBus.UnsubscribeEvent<GridStartupEvent>(EventSource.Local, this);
            _entityManager.EventBus.UnsubscribeEvent<GridRemovalEvent>(EventSource.Local, this);
        }

        public void Shutdown()
        {
            ShutdownWindowing();
        }

        private bool IsMainThread()
        {
            return Thread.CurrentThread == _gameThread;
        }
    }
}
