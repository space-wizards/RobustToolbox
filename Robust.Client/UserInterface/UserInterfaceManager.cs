using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    internal sealed partial class UserInterfaceManager : IUserInterfaceManagerInternal
    {
        [Dependency] private readonly IDependencyCollection _rootDependencies = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IFontManager _fontManager = default!;
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IClientGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        [ViewVariables] public InterfaceTheme ThemeDefaults { get; private set; } = default!;
        [ViewVariables]
        public Stylesheet? Stylesheet
        {
            get => _stylesheet;
            set
            {
                _stylesheet = value;

                foreach (var root in _roots)
                {
                    if (root.Stylesheet != null)
                    {
                        root.StylesheetUpdateRecursive();
                    }
                }
            }
        }

        [ViewVariables] public ViewportContainer MainViewport { get; private set; } = default!;
        [ViewVariables] public LayoutContainer StateRoot { get; private set; } = default!;
        [ViewVariables] public PopupContainer ModalRoot { get; private set; } = default!;
        [ViewVariables] public WindowRoot RootControl { get; private set; } = default!;
        [ViewVariables] public LayoutContainer WindowRoot { get; private set; } = default!;
        [ViewVariables] public LayoutContainer PopupRoot { get; private set; } = default!;
        [ViewVariables] public DropDownDebugConsole DebugConsole { get; private set; } = default!;
        [ViewVariables] public IDebugMonitors DebugMonitors => _debugMonitors;
        private DebugMonitors _debugMonitors = default!;

        private bool _rendering = true;

        private readonly Queue<Action> _deferQueue = new();
        private readonly Queue<Control> _styleUpdateQueue = new();
        private readonly Queue<Control> _measureUpdateQueue = new();
        private readonly Queue<Control> _arrangeUpdateQueue = new();
        private Stylesheet? _stylesheet;

        private ISawmill _sawmillUI = default!;

        public void Initialize()
        {
            _dependencies = new DependencyCollection(_rootDependencies);
            _configurationManager.OnValueChanged(CVars.DisplayUIScale, _uiScaleChanged, true);
            ThemeDefaults = new InterfaceThemeDummy();
            _initScaling();
            SetupControllers();
            _initializeCommon();

            DebugConsole = new DropDownDebugConsole();
            RootControl.AddChild(DebugConsole);
            DebugConsole.SetPositionInParent(ModalRoot.GetPositionInParent());

            _debugMonitors = new DebugMonitors(_gameTiming, _playerManager, _eyeManager, _inputManager, _stateManager,
                _clyde, _netManager, _mapManager);
            DebugConsole.BelowConsole.AddChild(_debugMonitors);

            _inputManager.SetInputCommand(EngineKeyFunctions.ShowDebugConsole,
                InputCmdHandler.FromDelegate(session => DebugConsole.Toggle()));

            _inputManager.SetInputCommand(EngineKeyFunctions.ShowDebugMonitors,
                InputCmdHandler.FromDelegate(enabled: session => { DebugMonitors.Visible = true; },
                    disabled: session => { DebugMonitors.Visible = false; }));

            _inputManager.SetInputCommand(EngineKeyFunctions.HideUI,
                InputCmdHandler.FromDelegate(
                    enabled: session => _rendering = false,
                    disabled: session => _rendering = true));

            _inputManager.UIKeyBindStateChanged += OnUIKeyBindStateChanged;
            _initThemes();
        }

        public void PostInitialize()
        {
            _initializeScreens();
            InitializeControllers();
        }
        private void _initializeCommon()
        {
            _sawmillUI = _logManager.GetSawmill("ui");

            RootControl = CreateWindowRoot(_clyde.MainWindow);
            RootControl.Name = "MainWindowRoot";

            _clyde.DestroyWindow += WindowDestroyed;
            _clyde.OnWindowFocused += ClydeOnWindowFocused;

            MainViewport = new MainViewportContainer(_eyeManager)
            {
                Name = "MainViewport"
            };
            RootControl.AddChild(MainViewport);

            StateRoot = new LayoutContainer
            {
                Name = "StateRoot",
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            RootControl.AddChild(StateRoot);

            WindowRoot = new LayoutContainer
            {
                Name = "WindowRoot",
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            RootControl.AddChild(WindowRoot);

            ModalRoot = new PopupContainer
            {
                Name = "ModalRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
            };
            RootControl.AddChild(ModalRoot);

            PopupRoot = new LayoutContainer
            {
                Name = "PopupRoot",
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            RootControl.AddChild(PopupRoot);

            _tooltip = new Tooltip();
            PopupRoot.AddChild(_tooltip);
            _tooltip.Visible = false;
        }

        public void InitializeTesting()
        {
            ThemeDefaults = new InterfaceThemeDummy();

            _initializeCommon();
        }

        public event Action<PostDrawUIRootEventArgs>? OnPostDrawUIRoot;

        public void DeferAction(Action action)
        {
            _deferQueue.Enqueue(action);
        }

        private void WindowDestroyed(WindowDestroyedEventArgs args)
        {
            DestroyWindowRoot(args.Window);
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            using (_prof.Group("Update"))
            {
                foreach (var root in _roots)
                {
                    using (_prof.Group("Root"))
                    {
                        var totalUpdated = root.DoFrameUpdateRecursive(args);

                        _prof.WriteValue("Total", ProfData.Int32(totalUpdated));
                    }
                }
            }

            // Process queued style & layout updates.
            using (_prof.Group("Style"))
            {
                var total = 0;
                while (_styleUpdateQueue.Count != 0)
                {
                    var control = _styleUpdateQueue.Dequeue();

                    if (control.Disposed)
                        continue;

                    control.DoStyleUpdate();
                    total += 1;
                }

                _prof.WriteValue("Total", ProfData.Int32(total));
            }

            using (_prof.Group("Measure"))
            {
                var total = 0;
                while (_measureUpdateQueue.Count != 0)
                {
                    var control = _measureUpdateQueue.Dequeue();

                    if (control.Disposed)
                        continue;

                    RunMeasure(control);
                    total += 1;
                }

                _prof.WriteValue("Total", ProfData.Int32(total));
            }

            using (_prof.Group("Arrange"))
            {
                var total = 0;
                while (_arrangeUpdateQueue.Count != 0)
                {
                    var control = _arrangeUpdateQueue.Dequeue();

                    if (control.Disposed)
                        continue;

                    RunArrange(control);
                    total += 1;
                }

                _prof.WriteValue("Total", ProfData.Int32(total));
            }

            using (_prof.Group("Controllers"))
            {
                UpdateControllers(args);
            }

            // count down tooltip delay if we're not showing one yet and
            // are hovering the mouse over a control without moving it
            if (_tooltipDelay != null && !showingTooltip)
            {
                _tooltipTimer += args.DeltaSeconds;
                if (_tooltipTimer >= _tooltipDelay)
                {
                    _showTooltip();
                }
            }

            if (_needUpdateActiveCursor)
            {
                _needUpdateActiveCursor = false;
                UpdateActiveCursor();
            }

            using (_prof.Group("Deferred actions"))
            {
                while (_deferQueue.TryDequeue(out var action))
                {
                    action();
                }
            }
        }

        private void _render(IRenderHandle renderHandle, ref int total, Control control, Vector2i position, Color modulate,
            UIBox2i? scissorBox)
        {
            if (!control.Visible)
            {
                return;
            }

            // Manual clip test with scissor region as optimization.
            var controlBox = UIBox2i.FromDimensions(position, control.PixelSize);

            if (scissorBox != null)
            {
                var clipMargin = control.RectDrawClipMargin;
                var clipTestBox = new UIBox2i(controlBox.Left - clipMargin, controlBox.Top - clipMargin,
                    controlBox.Right + clipMargin, controlBox.Bottom + clipMargin);

                if (!scissorBox.Value.Intersects(clipTestBox))
                {
                    return;
                }
            }

            var clip = control.RectClipContent;
            var scissorRegion = scissorBox;
            if (clip)
            {
                scissorRegion = controlBox;
                if (scissorBox != null)
                {
                    // Make the final scissor region a sub region of scissorBox
                    var s = scissorBox.Value;
                    var result = s.Intersection(scissorRegion.Value);
                    if (result == null)
                    {
                        // Uhm... No intersection so... don't draw anything at all?
                        return;
                    }

                    scissorRegion = result.Value;
                }

                renderHandle.SetScissor(scissorRegion);
            }

            total += 1;

            var handle = renderHandle.DrawingHandleScreen;
            handle.SetTransform(position, Angle.Zero, Vector2.One);
            modulate *= control.Modulate;

            if (_rendering || control.AlwaysRender)
            {
                // Handle modulation with care.
                var oldMod = handle.Modulate;
                handle.Modulate = modulate * control.ActualModulateSelf;
                control.DrawInternal(renderHandle);
                handle.Modulate = oldMod;
                handle.UseShader(null);
            }

            foreach (var child in control.Children)
            {
                _render(renderHandle, ref total, child, position + child.PixelPosition, modulate, scissorRegion);
            }

            if (clip)
            {
                renderHandle.SetScissor(scissorBox);
            }
        }

        public Color GetMainClearColor() => RootControl.ActualBgColor;

        ~UserInterfaceManager()
        {
            ClearWindows();
        }
    }
}
