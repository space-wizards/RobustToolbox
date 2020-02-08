using System;
using System.Collections.Generic;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface
{
    internal sealed class UserInterfaceManager : IDisposable, IUserInterfaceManagerInternal, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IClyde _displayManager;
        [Dependency] private readonly IClientConsole _console;
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;
#pragma warning restore 649

        public UITheme ThemeDefaults { get; private set; }
        public Stylesheet Stylesheet { get; set; }
        public Control KeyboardFocused { get; private set; }

        // When a control receives a mouse down it must also receive a mouse up and mouse moves, always.
        // So we keep track of which control is "focused" by the mouse.
        private Control _controlFocused;

        public LayoutContainer StateRoot { get; private set; }
        public PopupContainer ModalRoot { get; private set; }
        public Control CurrentlyHovered { get; private set; }
        public float UIScale { get; private set; } = 1;
        public Control RootControl { get; private set; }
        public LayoutContainer WindowRoot { get; private set; }
        public LayoutContainer PopupRoot { get; private set; }
        public DebugConsole DebugConsole { get; private set; }
        public IDebugMonitors DebugMonitors => _debugMonitors;
        private DebugMonitors _debugMonitors;

        private readonly List<Control> _modalStack = new List<Control>();

        private bool _rendering = true;
        private float _tooltipTimer;
        private Tooltip _tooltip;
        private const float TooltipDelay = 1;

        private readonly Queue<Control> _styleUpdateQueue = new Queue<Control>();
        private readonly Queue<Control> _layoutUpdateQueue = new Queue<Control>();

        public void Initialize()
        {
            UIScale = _configurationManager.GetCVar<float>("display.uiScale");
            ThemeDefaults = new UIThemeDummy();

            _initializeCommon();

            DebugConsole = new DebugConsole(_console, _resourceManager);
            RootControl.AddChild(DebugConsole);

            _debugMonitors = new DebugMonitors(_gameTiming, _playerManager, _eyeManager, _inputManager, _stateManager, _displayManager, _netManager, _mapManager);
            RootControl.AddChild(_debugMonitors);

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
        }

        private void _initializeCommon()
        {
            RootControl = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            RootControl.Size = _displayManager.ScreenSize / UIScale;
            _displayManager.OnWindowResized += args => _updateRootSize();

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

            PopupRoot = new LayoutContainer
            {
                Name = "PopupRoot",
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            RootControl.AddChild(PopupRoot);

            ModalRoot = new PopupContainer
            {
                Name = "ModalRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
            };
            RootControl.AddChild(ModalRoot);

            _tooltip = new Tooltip();
            PopupRoot.AddChild(_tooltip);
            _tooltip.Visible = false;
        }

        public void InitializeTesting()
        {
            ThemeDefaults = new UIThemeDummy();

            _initializeCommon();
        }

        public void Dispose()
        {
            RootControl?.Dispose();
        }

        public void Update(FrameEventArgs args)
        {
            RootControl.DoUpdate(args);
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            RootControl.DoFrameUpdate(args);

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

            while (_layoutUpdateQueue.Count != 0)
            {
                var control = _layoutUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                control.DoLayoutUpdate();
            }

            _tooltipTimer -= args.DeltaSeconds;
            if (_tooltipTimer <= 0)
            {
                _showTooltip();
            }
        }

        public void KeyBindDown(BoundKeyEventArgs args)
        {
            var control = MouseGetControl(args.PointerLocation.Position);
            if (args.CanFocus)
            {
                // If we have a modal open and the mouse down was outside it, close said modal.
                if (_modalStack.Count != 0)
                {
                    var top = _modalStack[_modalStack.Count - 1];
                    var offset = args.PointerLocation.Position - top.GlobalPixelPosition;
                    if (!top.HasPoint(offset / UIScale))
                    {
                        RemoveModal(top);
                        return;
                    }
                }

                ReleaseKeyboardFocus();

                if (control == null)
                {
                    return;
                }

                _controlFocused = control;

                if (_controlFocused.CanKeyboardFocus && _controlFocused.KeyboardFocusOnClick)
                {
                    _controlFocused.GrabKeyboardFocus();
                }
            }
            else if (KeyboardFocused != null)
            {
                control = KeyboardFocused;
            }

            if (control == null)
            {
                return;
            }

            var guiArgs = new GUIBoundKeyEventArgs(args.Function, args.State, args.PointerLocation, args.CanFocus,
                args.PointerLocation.Position / UIScale - control.GlobalPosition,
                args.PointerLocation.Position - control.GlobalPixelPosition);

            _doGuiInput(control, guiArgs, (c, ev) => c.KeyBindDown(ev));

            if (args.CanFocus)
            {
                args.Handle();
            }
        }

        public void KeyBindUp(BoundKeyEventArgs args)
        {
            var control = _controlFocused ?? KeyboardFocused ?? MouseGetControl(args.PointerLocation.Position);
            if (control == null)
            {
                return;
            }

            var guiArgs = new GUIBoundKeyEventArgs(args.Function, args.State, args.PointerLocation, args.CanFocus,
                args.PointerLocation.Position / UIScale - control.GlobalPosition,
                args.PointerLocation.Position - control.GlobalPixelPosition);

            _doGuiInput(control, guiArgs, (c, ev) => c.KeyBindUp(ev));
            _controlFocused = null;

            // Always mark this as handled.
            // The only case it should not be is if we do not have a control to click on,
            // in which case we never reach this.
            args.Handle();
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _resetTooltipTimer();
            // Update which control is considered hovered.
            var newHovered = MouseGetControl(mouseMoveEventArgs.Position);
            if (newHovered != CurrentlyHovered)
            {
                _clearTooltip();
                CurrentlyHovered?.MouseExited();
                CurrentlyHovered = newHovered;
                CurrentlyHovered?.MouseEntered();
            }

            var target = _controlFocused ?? newHovered;
            if (target != null)
            {
                var guiArgs = new GUIMouseMoveEventArgs(mouseMoveEventArgs.Relative / UIScale,
                    target,
                    mouseMoveEventArgs.Position / UIScale, mouseMoveEventArgs.Position,
                    mouseMoveEventArgs.Position / UIScale - target.GlobalPosition,
                    mouseMoveEventArgs.Position - target.GlobalPixelPosition);

                _doMouseGuiInput(target, guiArgs, (c, ev) => c.MouseMove(ev));
            }
        }

        public void MouseWheel(MouseWheelEventArgs args)
        {
            var control = MouseGetControl(args.Position);
            if (control == null)
            {
                return;
            }

            args.Handle();

            var guiArgs = new GUIMouseWheelEventArgs(args.Delta, control,
                args.Position / UIScale, args.Position,
                args.Position / UIScale - control.GlobalPosition, args.Position - control.GlobalPixelPosition);

            _doMouseGuiInput(control, guiArgs, (c, ev) => c.MouseWheel(ev), true);
        }

        public void TextEntered(TextEventArgs textEvent)
        {
            if (KeyboardFocused == null)
            {
                return;
            }

            var guiArgs = new GUITextEventArgs(KeyboardFocused, textEvent.CodePoint);
            KeyboardFocused.TextEntered(guiArgs);
        }

        public void DisposeAllComponents()
        {
            RootControl.DisposeAllChildren();
        }

        public void Popup(string contents, string title = "Alert!")
        {
            var popup = new SS14Window
            {
                Title = title
            };

            popup.Contents.AddChild(new Label {Text = contents});
            popup.OpenCenteredMinSize();
        }

        public Control MouseGetControl(Vector2 coordinates)
        {
            return _mouseFindControlAtPos(RootControl, coordinates);
        }

        /// <inheritdoc />
        public void GrabKeyboardFocus(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (!control.CanKeyboardFocus)
            {
                throw new ArgumentException("Control cannot get keyboard focus.", nameof(control));
            }

            if (control == KeyboardFocused)
            {
                return;
            }

            ReleaseKeyboardFocus();

            KeyboardFocused = control;

            KeyboardFocused.FocusEntered();
        }

        public void ReleaseKeyboardFocus()
        {
            var oldFocused = KeyboardFocused;
            oldFocused?.FocusExited();
            KeyboardFocused = null;
        }

        public void ReleaseKeyboardFocus(Control ifControl)
        {
            if (ifControl == null)
            {
                throw new ArgumentNullException(nameof(ifControl));
            }

            if (ifControl == KeyboardFocused)
            {
                ReleaseKeyboardFocus();
            }
        }

        public void ControlHidden(Control control)
        {
            // Does the same thing but it could later be changed so..
            ControlRemovedFromTree(control);
        }

        public void ControlRemovedFromTree(Control control)
        {
            ReleaseKeyboardFocus(control);
            RemoveModal(control);
            if (control == CurrentlyHovered)
            {
                control.MouseExited();
                CurrentlyHovered = null;
            }

            if (control == _controlFocused)
            {
                _controlFocused = null;
            }
        }

        public void PushModal(Control modal)
        {
            _modalStack.Add(modal);
        }

        public void RemoveModal(Control modal)
        {
            if (_modalStack.Remove(modal))
            {
                modal.ModalRemoved();
            }
        }

        public void Render(IRenderHandle renderHandle)
        {
            if (!_rendering)
            {
                return;
            }

            _render(renderHandle, RootControl, Vector2i.Zero, Color.White, null);
        }

        public void QueueStyleUpdate(Control control)
        {
            _styleUpdateQueue.Enqueue(control);
        }

        public void QueueLayoutUpdate(Control control)
        {
            _layoutUpdateQueue.Enqueue(control);
        }

        private static void _render(IRenderHandle renderHandle, Control control, Vector2i position, Color modulate,
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

            control.DrawInternal(renderHandle);
            foreach (var child in control.Children)
            {
                _render(renderHandle, child, position + child.PixelPosition, modulate, scissorRegion);
            }

            if (clip)
            {
                renderHandle.SetScissor(scissorBox);
            }
        }

        private Control _mouseFindControlAtPos(Control control, Vector2 position)
        {
            for (var i = control.ChildCount - 1; i >= 0; i--)
            {
                var child = control.GetChild(i);
                if (!child.Visible || (child.RectClipContent && !child.PixelRect.Contains((Vector2i) position)))
                {
                    continue;
                }

                var maybeFoundOnChild = _mouseFindControlAtPos(child, position - child.PixelPosition);
                if (maybeFoundOnChild != null)
                {
                    return maybeFoundOnChild;
                }
            }

            if (control.MouseFilter != Control.MouseFilterMode.Ignore && control.HasPoint(position / UIScale))
            {
                return control;
            }

            return null;
        }

        private static void _doMouseGuiInput<T>(Control control, T guiEvent, Action<Control, T> action,
            bool ignoreStop = false)
            where T : GUIMouseEventArgs
        {
            while (control != null)
            {
                if (control.MouseFilter != Control.MouseFilterMode.Ignore)
                {
                    action(control, guiEvent);

                    if (guiEvent.Handled || (!ignoreStop && control.MouseFilter == Control.MouseFilterMode.Stop))
                    {
                        break;
                    }
                }

                guiEvent.RelativePosition += control.Position;
                guiEvent.RelativePixelPosition += control.PixelPosition;
                control = control.Parent;
                guiEvent.SourceControl = control;
            }
        }

        private static void _doGuiInput<T>(Control control, T guiEvent, Action<Control, T> action,
            bool ignoreStop = false)
            where T : GUIBoundKeyEventArgs
        {
            while (control != null)
            {
                if (control.MouseFilter != Control.MouseFilterMode.Ignore)
                {
                    action(control, guiEvent);

                    if (guiEvent.Handled || (!ignoreStop && control.MouseFilter == Control.MouseFilterMode.Stop))
                    {
                        break;
                    }
                }

                guiEvent.RelativePosition += control.Position;
                guiEvent.RelativePixelPosition += control.PixelPosition;
                control = control.Parent;
            }
        }

        private void _clearTooltip()
        {
            _tooltip.Visible = false;
            _resetTooltipTimer();
        }

        private void _resetTooltipTimer()
        {
            _tooltipTimer = TooltipDelay;
        }

        private void _showTooltip()
        {
            var hovered = CurrentlyHovered;
            if (hovered == null || string.IsNullOrWhiteSpace(hovered.ToolTip))
            {
                return;
            }

            _tooltip.Visible = true;
            _tooltip.Text = hovered.ToolTip;
            LayoutContainer.SetPosition(_tooltip, _inputManager.MouseScreenPosition);
            LayoutContainer.SetSize(_tooltip, _tooltip.CustomMinimumSize);

            var (right, bottom) = _tooltip.Position + _tooltip.Size;

            if (right > RootControl.Size.X)
            {
                LayoutContainer.SetPosition(_tooltip, (RootControl.Size.X - _tooltip.Size.X, _tooltip.Position.Y));
            }

            if (bottom > RootControl.Size.Y)
            {
                LayoutContainer.SetPosition(_tooltip, (_tooltip.Position.X, RootControl.Size.Y - _tooltip.Size.Y));
            }
        }

        void IPostInjectInit.PostInject()
        {
            _configurationManager.RegisterCVar("display.uiScale", 1f, CVar.ARCHIVE, _uiScaleChanged);
        }

        private void _uiScaleChanged(float newValue)
        {
            UIScale = newValue;

            if (RootControl == null)
            {
                return;
            }

            _propagateUIScaleChanged(RootControl);
            _updateRootSize();
        }

        private static void _propagateUIScaleChanged(Control control)
        {
            control.UIScaleChanged();

            foreach (var child in control.Children)
            {
                _propagateUIScaleChanged(child);
            }
        }

        private void _updateRootSize()
        {
            RootControl.Size = _displayManager.ScreenSize / UIScale;
        }

        /// <summary>
        ///     Converts
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        private void OnUIKeyBindStateChanged(BoundKeyEventArgs args)
        {
            if (!args.CanFocus && KeyboardFocused != null)
            {
                args.Handle();
            }
            if (args.State == BoundKeyState.Down)
            {
                KeyBindDown(args);
            }
            else
            {
                KeyBindUp(args);
            }
        }
    }
}
