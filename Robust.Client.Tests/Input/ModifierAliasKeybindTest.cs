using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using NUnit.Framework;
using Robust.Client.Input;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Tests.Input
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public sealed class ModifierAliasKeybindTest
    {
        private sealed class TestInputManager : InputManager
        {
            public void PublicInitialize() => Initialize();
        }

        private sealed class NullConsoleHost : IConsoleHost
        {
            public bool IsServer => false;
            public IConsoleShell LocalShell => DummyConsoleShell.Instance;
            public IReadOnlyDictionary<string, IConsoleCommand> AvailableCommands =>
                new Dictionary<string, IConsoleCommand>();

            public event ConAnyCommandCallback? AnyCommandExecuted
            {
                add { }
                remove { }
            }

            public event EventHandler? ClearText
            {
                add { }
                remove { }
            }

            public void LoadConsoleCommands() { }
            public void RegisterCommand(string command, string description, string help, ConCommandCallback callback,
                bool requireServerOrSingleplayer = false)
            { }
            public void RegisterCommand(string command, string description, string help, ConCommandCallback callback,
                ConCommandCompletionCallback completionCallback, bool requireServerOrSingleplayer = false)
            { }
            public void RegisterCommand(string command, string description, string help, ConCommandCallback callback,
                ConCommandCompletionAsyncCallback completionCallback, bool requireServerOrSingleplayer = false)
            { }
            public void RegisterCommand(string command, ConCommandCallback callback, bool requireServerOrSingleplayer = false) { }
            public void RegisterCommand(string command, ConCommandCallback callback, ConCommandCompletionCallback completionCallback,
                bool requireServerOrSingleplayer = false)
            { }
            public void RegisterCommand(string command, ConCommandCallback callback, ConCommandCompletionAsyncCallback completionCallback,
                bool requireServerOrSingleplayer = false)
            { }
            public void RegisterCommand(IConsoleCommand command) { }
            public void BeginRegistrationRegion() { }
            public void EndRegistrationRegion() { }
            public void UnregisterCommand(string command) { }
            public IConsoleShell GetSessionShell(ICommonSession session) => LocalShell;
            public void ExecuteCommand(string command) { }
            public void AppendCommand(string command) { }
            public void InsertCommand(string command) { }
            public void CommandBufferExecute() { }
            public void ExecuteCommand(ICommonSession? session, string command) { }
            public void RemoteExecuteCommand(ICommonSession? session, string command) { }
            public void WriteLine(ICommonSession? session, string text) { }
            public void WriteLine(ICommonSession? session, FormattedMessage msg) { }
            public void WriteError(ICommonSession? session, string text) { }
            public void ClearLocalConsole() { }
        }

        private sealed class NullResourceManager : IResourceManager
        {
            public IWritableDirProvider UserData { get; } = new VirtualWritableDirProvider();

            public void AddRoot(ResPath prefix, IContentRoot loader) { }
            public Stream ContentFileRead(ResPath path) => throw new NotSupportedException();
            public Stream ContentFileRead(string path) => throw new NotSupportedException();
            public bool ContentFileExists(ResPath path) => false;
            public bool ContentFileExists(string path) => false;
            public bool TryContentFileRead(ResPath? path,
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Stream? fileStream)
            {
                fileStream = null;
                return false;
            }

            public bool TryContentFileRead(string path,
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Stream? fileStream)
            {
                fileStream = null;
                return false;
            }

            public IEnumerable<ResPath> ContentFindFiles(ResPath? path) => Array.Empty<ResPath>();
            public IEnumerable<ResPath> ContentFindFiles(string path) => Array.Empty<ResPath>();
            public IEnumerable<string> ContentGetDirectoryEntries(ResPath path) => Array.Empty<string>();
            public IEnumerable<ResPath> GetContentRoots() => Array.Empty<ResPath>();
        }

        private sealed class DummyConsoleShell : IConsoleShell
        {
            public static DummyConsoleShell Instance { get; } = new();
            public IConsoleHost ConsoleHost => null!;
            public ICommonSession? Player => null;
            public bool IsLocal => true;
            public bool IsServer => false;
            public void ExecuteCommand(string command) { }
            public void RemoteExecuteCommand(string command) { }
            public void WriteLine(string text) { }
            public void WriteLine(FormattedMessage message) { }
            public void WriteError(string text) { }
            public void Clear() { }
        }

        private sealed class NullUserInterfaceManager : IUserInterfaceManagerInternal
        {
            public void Initialize() { }
            public void FrameUpdate(FrameEventArgs args) { }
            public bool HandleCanFocusDown(ScreenCoordinates pointerPosition,
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out (Control control, Vector2i rel)? hitData)
            {
                hitData = null;
                return false;
            }

            public void HandleCanFocusUp() { }
            public void KeyBindDown(BoundKeyEventArgs args) { }
            public void KeyBindUp(BoundKeyEventArgs args) { }
            public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs) { }
            public void MouseWheel(MouseWheelEventArgs args) { }
            public void TextEntered(TextEnteredEventArgs textEnteredEvent) { }
            public void TextEditing(TextEditingEventArgs textEvent) { }
            public void ControlHidden(Control control) { }
            public void ControlRemovedFromTree(Control control) { }
            public void RemoveModal(Control modal) { }
            public void Render(IRenderHandle renderHandle) { }
            public void QueueStyleUpdate(Control control) { }
            public void QueueMeasureUpdate(Control control) { }
            public void QueueArrangeUpdate(Control control) { }
            public void CursorChanged(Control control) { }
            public void HideTooltipFor(Control control) { }
            public Control? GetSuppliedTooltipFor(Control control) => null;
            public Vector2? CalcRelativeMousePositionFor(Control control, ScreenCoordinates mousePos) => null;
            public Color GetMainClearColor() => Color.Black;
            public void RenderControl(IRenderHandle renderHandle, ref int total, Control control, Vector2i position,
                Color modulate, UIBox2i? scissorBox, Matrix3x2 coordinateTransform)
            { }

            public void InitializeTesting() { }
            public InterfaceTheme ThemeDefaults => default!;
            public Stylesheet? Stylesheet { get; set; }
            public Control? KeyboardFocused => null;
            public Control? ControlFocused { get; set; }
            public void PostInitialize() { }
            public ViewportContainer MainViewport => default!;
            public LayoutContainer StateRoot => default!;
            public LayoutContainer WindowRoot => default!;
            public LayoutContainer PopupRoot => default!;
            public PopupContainer ModalRoot => default!;
            public Control? CurrentlyHovered => null;
            public float DefaultUIScale => 1f;
            public WindowRoot RootControl => default!;
            public IDebugMonitors DebugMonitors => default!;
            public void Popup(string contents, string? title = null, bool clipboardButton = true) { }
            public Control? MouseGetControl(ScreenCoordinates coordinates) => null;
            public ScreenCoordinates MousePositionScaled => default;
            public ScreenCoordinates ScreenToUIPosition(ScreenCoordinates coordinates) => coordinates;
            public void GrabKeyboardFocus(Control control) { }
            public void ReleaseKeyboardFocus() { }
            public void ReleaseKeyboardFocus(Control ifControl) { }
            public ICursor? WorldCursor { get; set; }
            public void PushModal(Control modal) { }
            public WindowRoot CreateWindowRoot(IClydeWindow window) => default!;
            public void DestroyWindowRoot(IClydeWindow window) { }
            public WindowRoot? GetWindowRoot(IClydeWindow window) => null;
            public IEnumerable<UIRoot> AllRoots => Array.Empty<UIRoot>();

            public event Action<PostDrawUIRootEventArgs>? OnPostDrawUIRoot
            {
                add { }
                remove { }
            }

            public void DeferAction(Action action) => action();

            public event Action<Control>? OnKeyBindDown
            {
                add { }
                remove { }
            }

            public void SetClickSound(Robust.Shared.Audio.Sources.IAudioSource? source) { }
            public void ClickSound() { }
            public void SetHoverSound(Robust.Shared.Audio.Sources.IAudioSource? source) { }
            public void HoverSound() { }
            public void SetHovered(Control? control) { }
            public void UpdateHovered() { }
            public void RenderControl(in Control.ControlRenderArguments args, Control control) { }
            public void RenderControl(IRenderHandle handle, Control control, Vector2i position) { }
            public ISawmill ControlSawmill => null!;

            public T GetUIController<T>() where T : Robust.Client.UserInterface.Controllers.UIController, new() => new();

            public Robust.Client.UserInterface.Themes.UITheme CurrentTheme => default!;
            public Robust.Client.UserInterface.Themes.UITheme GetTheme(string name) => default!;
            public Robust.Client.UserInterface.Themes.UITheme GetThemeOrDefault(string name) => default!;
            public void SetActiveTheme(string themeName) { }
            public Robust.Client.UserInterface.Themes.UITheme DefaultTheme => default!;
            public void SetDefaultTheme(string themeId) { }

            public UIScreen? ActiveScreen => null;
            public void LoadScreen<T>() where T : UIScreen, new() { }
            void IUserInterfaceManager.LoadScreenInternal(Type type) { }
            public void UnloadScreen() { }
            public T? GetActiveUIWidgetOrNull<T>() where T : UIWidget, new() => null;
            public T GetActiveUIWidget<T>() where T : UIWidget, new() => new();

            public event Action<(UIScreen? Old, UIScreen? New)>? OnScreenChanged
            {
                add { }
                remove { }
            }

            public T CreatePopup<T>() where T : Popup, new() => new();
            public bool RemoveFirstPopup<T>() where T : Popup, new() => false;
            public bool TryGetFirstPopup<T>(out T? popup) where T : Popup, new()
            {
                popup = null;
                return false;
            }
            public bool TryGetFirstPopup(Type type, out Popup? popup)
            {
                popup = null;
                return false;
            }

            public bool RemoveFirstWindow<T>() where T : BaseWindow, new() => false;
            public T CreateWindow<T>() where T : BaseWindow, new() => new();
            public void ClearWindows() { }
            public T GetFirstWindow<T>() where T : BaseWindow, new() => new();
            public bool TryGetFirstWindow<T>(out T? window) where T : BaseWindow, new()
            {
                window = null;
                return false;
            }
            public bool TryGetFirstWindow(Type type, out BaseWindow? window)
            {
                window = null;
                return false;
            }
        }

        [Test]
        public void KeybindMatchesWithCtrlAliasFromModifiers()
        {
            var deps = IoCManager.InitThread();
            deps.Clear();

            deps.Register<IResourceManager, NullResourceManager>();
            deps.Register<ILogManager, LogManager>();
            deps.Register<IGameTiming, GameTiming>();
            deps.Register<IReflectionManager, Robust.Client.Reflection.ClientReflectionManager>();
            deps.Register<ISerializationManager, SerializationManager>();
            deps.Register<IConsoleHost, NullConsoleHost>();
            deps.Register<IUserInterfaceManagerInternal, NullUserInterfaceManager>();
            deps.Register<IUserInterfaceManager, NullUserInterfaceManager>();

            deps.BuildGraph();

            var reflection = deps.Resolve<IReflectionManager>();
            reflection.Initialize();

            var input = new TestInputManager();
            IoCManager.InjectDependencies(input);
            input.PublicInitialize();

            input.Contexts.ActiveContext.AddFunction(EngineKeyFunctions.Use);

            input.RegisterBinding(new KeyBindingRegistration
            {
                Function = EngineKeyFunctions.Use,
                BaseKey = Keyboard.Key.A,
                Mod1 = Keyboard.Key.Control,
                Type = KeyBindingType.State
            });

            var triggered = false;
            input.KeyBindStateChanged += args =>
            {
                if (args.KeyEventArgs.Function == EngineKeyFunctions.Use &&
                    args.KeyEventArgs.State == BoundKeyState.Down)
                {
                    triggered = true;
                }
            };

            var ctrlFromCaps = new KeyEventArgs(
                Keyboard.Key.CapsLock,
                false,
                alt: false,
                control: true,
                shift: false,
                system: false,
                scanCode: 0);

            input.KeyDown(ctrlFromCaps);

            var aKey = new KeyEventArgs(
                Keyboard.Key.A,
                false,
                alt: false,
                control: true,
                shift: false,
                system: false,
                scanCode: 0);

            input.KeyDown(aKey);

            Assert.That(triggered, Is.True);

            IoCManager.Clear();
        }
    }
}
