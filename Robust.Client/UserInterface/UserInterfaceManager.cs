using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    internal sealed partial class UserInterfaceManager : IUserInterfaceManagerInternal
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IFontManager _fontManager = default!;
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IUIControllerManagerInternal _controllers = default!;

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

        private readonly Queue<Control> _styleUpdateQueue = new();
        private readonly Queue<Control> _measureUpdateQueue = new();
        private readonly Queue<Control> _arrangeUpdateQueue = new();
        private Stylesheet? _stylesheet;

        public void Initialize()
        {
            _configurationManager.OnValueChanged(CVars.DisplayUIScale, _uiScaleChanged, true);
            ThemeDefaults = new InterfaceThemeDummy();

            _initScaling();
            _initializeCommon();

            DebugConsole = new DropDownDebugConsole();
            RootControl.AddChild(DebugConsole);

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
            _stateManager.OnStateChanged += OnStateUpdated;
            _initThemes();
        }

        private void _initializeCommon()
        {
            RootControl = CreateWindowRoot(_clyde.MainWindow);
            RootControl.Name = "MainWindowRoot";
            _clyde.DestroyWindow += WindowDestroyed;

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

        private void WindowDestroyed(WindowDestroyedEventArgs args)
        {
            DestroyWindowRoot(args.Window);
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            // Process queued style & layout updates.
            while (_styleUpdateQueue.Count != 0)
            {
                var control = _styleUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                control.DoStyleUpdate();
            }

            while (_measureUpdateQueue.Count != 0)
            {
                var control = _measureUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                RunMeasure(control);
            }

            while (_arrangeUpdateQueue.Count != 0)
            {
                var control = _arrangeUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                RunArrange(control);
            }

            _controllers.FrameUpdate(args);

            foreach (var root in _roots)
            {
                root.DoFrameUpdate(args);
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
        }

        private void _render(IRenderHandle renderHandle, Control control, Vector2i position, Color modulate,
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

            var handle = renderHandle.DrawingHandleScreen;
            handle.SetTransform(position, Angle.Zero, Vector2.One);
            modulate *= control.Modulate;
            handle.Modulate = modulate * control.ActualModulateSelf;
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

            if (_rendering || control.AlwaysRender)
            {
                control.DrawInternal(renderHandle);
                handle.UseShader(null);
            }

            foreach (var child in control.Children)
            {
                _render(renderHandle, child, position + child.PixelPosition, modulate, scissorRegion);
            }

            if (clip)
            {
                renderHandle.SetScissor(scissorBox);
            }
        }

        public Color GetMainClearColor() => RootControl.ActualBgColor;

        ~UserInterfaceManager()
        {
            CleanupWindowData();
        }
    }
}
