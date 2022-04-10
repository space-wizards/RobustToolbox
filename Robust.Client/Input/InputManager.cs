using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using static Robust.Client.Input.Keyboard;

namespace Robust.Client.Input
{
    [Virtual]
    internal class InputManager : IInputManager
    {
        // This is for both userdata and resources.
        private const string KeybindsPath = "/keybinds.yml";

        [ViewVariables] public bool Enabled { get; set; } = true;

        [ViewVariables] public virtual ScreenCoordinates MouseScreenPosition => default;

        [Dependency] private readonly IResourceManager _resourceMan = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _uiMgr = default!;
        [Dependency] private readonly IConsoleHost _console = default!;

        private bool _currentlyFindingViewport;

        private readonly List<KeyBindingRegistration> _defaultRegistrations = new();

        private readonly Dictionary<BoundKeyFunction, InputCmdHandler> _commands =
            new();

        private readonly Dictionary<BoundKeyFunction, List<KeyBinding>> _bindingsByFunction
            = new();

        // For knowing what to write to config.
        private readonly HashSet<BoundKeyFunction> _modifiedKeyFunctions = new();

        [ViewVariables] private readonly List<KeyBinding> _bindings = new();
        private readonly bool[] _keysPressed = new bool[256];

        /// <inheritdoc />
        [ViewVariables]
        public BoundKeyMap NetworkBindMap { get; private set; } = default!;

        /// <inheritdoc />
        [ViewVariables]
        public IInputContextContainer Contexts { get; } = new InputContextContainer();

        /// <inheritdoc />
        public event Func<BoundKeyEventArgs, bool>? UIKeyBindStateChanged;

        /// <inheritdoc />
        public event Action<ViewportBoundKeyEventArgs>? KeyBindStateChanged;

        public IEnumerable<BoundKeyFunction> DownKeyFunctions => _bindings
            .Where(x => x.State == BoundKeyState.Down)
            .Select(x => x.Function)
            .ToList();

        public virtual string GetKeyName(Key key)
        {
            return string.Empty;
        }

        public string GetKeyFunctionButtonString(BoundKeyFunction function)
        {
            if (!TryGetKeyBinding(function, out var bind))
            {
                return Loc.GetString("<not bound>");
            }

            return bind.GetKeyString();
        }

        public IEnumerable<IKeyBinding> AllBindings => _bindings;
        public event KeyEventAction? FirstChanceOnKeyEvent;
        public event Action<IKeyBinding>? OnKeyBindingAdded;
        public event Action<IKeyBinding>? OnKeyBindingRemoved;
        public event Action? OnInputModeChanged;

        /// <inheritdoc />
        public void Initialize()
        {
            NetworkBindMap = new BoundKeyMap(_reflectionManager);
            NetworkBindMap.PopulateKeyFunctionsMap();

            EngineContexts.SetupContexts(Contexts);

            Contexts.ContextChanged += OnContextChanged;

            var path = new ResourcePath(KeybindsPath);
            if (_resourceMan.UserData.Exists(path))
            {
                LoadKeyFile(path, true);
            }

            if (_resourceMan.ContentFileExists(path))
            {
                LoadKeyFile(path, false);
            }
        }

        public void SaveToUserData()
        {
            var mapping = new MappingDataNode();
            var serializationManager = IoCManager.Resolve<ISerializationManager>();

            var modifiedBindings = _modifiedKeyFunctions
                .Select(p => _bindingsByFunction[p])
                .SelectMany(p => p)
                .Select(p => new KeyBindingRegistration
                {
                    Function = p.Function.FunctionName,
                    BaseKey = p.BaseKey,
                    Mod1 = p.Mod1,
                    Mod2 = p.Mod2,
                    Mod3 = p.Mod3,
                    Priority = p.Priority,
                    Type = p.BindingType,
                    CanFocus = p.CanFocus,
                    CanRepeat = p.CanRepeat,
                    AllowSubCombs = p.AllowSubCombs
                }).ToArray();

            var leaveEmpty = _modifiedKeyFunctions
                .Where(p => _bindingsByFunction[p].Count == 0)
                .ToArray();

            mapping.Add("version", new ValueDataNode("1"));
            mapping.Add("binds", serializationManager.WriteValue(modifiedBindings));
            mapping.Add("leaveEmpty", serializationManager.WriteValue(leaveEmpty));

            var path = new ResourcePath(KeybindsPath);
            using var writer = _resourceMan.UserData.OpenWriteText(path);
            var stream = new YamlStream {new(mapping.ToYaml())};
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        private void OnContextChanged(object? sender, ContextChangedEventArgs args)
        {
            // keyup any commands that are not in the new contexts, because it will not exist in the new context and get filtered. Luckily
            // the diff does not have to be symmetrical, otherwise instead of 'A \ B' we allocate all the things with '(A \ B) ∪ (B \ A)'
            // It should be OK to artificially keyup these, because in the future the organic keyup will be blocked (either the context
            // does not have the binding, or the double keyup check in UpBind will block it).
            if (args.OldContext == null)
            {
                return;
            }

            IEnumerable<BoundKeyFunction> enumerable = args.OldContext;
            if (args.NewContext != null)
            {
                enumerable = enumerable.Except(args.NewContext);
            }

            foreach (var function in enumerable)
            {
                var bind = _bindings.Find(binding => binding.Function == function);
                if (bind == null || bind.State == BoundKeyState.Up)
                {
                    continue;
                }

                SetBindState(bind, BoundKeyState.Up);
            }
        }

        /// <inheritdoc />
        public void KeyDown(KeyEventArgs args)
        {
            if (!Enabled || args.Key == Key.Unknown)
            {
                return;
            }

            FirstChanceOnKeyEvent?.Invoke(args, args.IsRepeat ? KeyEventType.Repeat : KeyEventType.Down);

            if (args.Handled)
            {
                return;
            }

            _keysPressed[(int) args.Key] = true;

            PackedKeyCombo matchedCombo = default;

            var bindsDown = new List<KeyBinding>();
            var hasCanFocus = false;
            var hasAllowSubCombs = false;

            // bindings are ordered with larger combos before single key bindings so combos have priority.
            foreach (var binding in _bindings)
            {
                // check if our binding is even in the active context
                if (binding.BindingType != KeyBindingType.Command && !Contexts.ActiveContext.FunctionExistsHierarchy(binding.Function))
                    continue;

                if (PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    // this statement *should* always be true first
                    // Keep triggering keybinds of the same PackedKeyCombo until Handled or no bindings left
                    if ((matchedCombo == default || binding.PackedKeyCombo == matchedCombo) &&
                        PackedContainsKey(binding.PackedKeyCombo, args.Key))
                    {
                        matchedCombo = binding.PackedKeyCombo;

                        bindsDown.Add(binding);

                        hasCanFocus |= binding.CanFocus;
                        hasAllowSubCombs |= binding.AllowSubCombs;

                    }
                    else if (PackedIsSubPattern(matchedCombo, binding.PackedKeyCombo))
                    {
                        if (hasAllowSubCombs)
                        {
                            bindsDown.Add(binding);
                        }
                        else
                        {
                            // kill any lower level matches
                            UpBind(binding);
                        }
                    }
                }
            }

            var uiOnly = false;
            if (hasCanFocus)
            {
                uiOnly = _uiMgr.HandleCanFocusDown(MouseScreenPosition, out _);
            }

            if (_uiMgr.KeyboardFocused is IRawInputControl rawInput)
            {
                var block = RaiseRawKeyInput(args, rawInput, args.IsRepeat ? RawKeyAction.Repeat : RawKeyAction.Down);

                if (block)
                    return;
            }

            foreach (var binding in bindsDown)
            {
                if (DownBind(binding, uiOnly, args.IsRepeat))
                {
                    break;
                }
            }
        }

        private bool RaiseRawKeyInput(KeyEventArgs args, IRawInputControl rawInput, RawKeyAction action)
        {
            DebugTools.AssertNotNull(_uiMgr.KeyboardFocused);

            var mousePos = _uiMgr.CalcRelativeMousePositionFor(_uiMgr.KeyboardFocused!, _uiMgr.MousePositionScaled);
            var keyEvent = new GuiRawKeyEvent(
                args.Key,
                args.ScanCode,
                action,
                (Vector2i) (mousePos ?? Vector2.Zero));

            var block = rawInput.RawKeyEvent(keyEvent);
            return block;
        }

        /// <inheritdoc />
        public void KeyUp(KeyEventArgs args)
        {
            if (args.Key == Key.Unknown)
            {
                return;
            }

            FirstChanceOnKeyEvent?.Invoke(args, KeyEventType.Up);

            var hasCanFocus = false;
            foreach (var binding in _bindings)
            {
                // check if our binding is even in the active context
                if (!Contexts.ActiveContext.FunctionExistsHierarchy(binding.Function))
                    continue;

                if (PackedContainsKey(binding.PackedKeyCombo, args.Key) &&
                    PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    hasCanFocus |= binding.CanFocus;
                    UpBind(binding);
                }
            }

            _keysPressed[(int) args.Key] = false;

            if (hasCanFocus)
            {
                _uiMgr.HandleCanFocusUp();
            }

            if (_uiMgr.KeyboardFocused is IRawInputControl rawInput)
                RaiseRawKeyInput(args, rawInput, RawKeyAction.Up);
        }

        private bool DownBind(KeyBinding binding, bool uiOnly, bool isRepeat)
        {
            if (binding.State == BoundKeyState.Down)
            {
                if (isRepeat)
                {
                    if (binding.CanRepeat)
                    {
                        return SetBindState(binding, BoundKeyState.Down, uiOnly);
                    }

                    return true;
                }

                if (binding.BindingType == KeyBindingType.Toggle)
                {
                    return SetBindState(binding, BoundKeyState.Up);
                }
            }
            else
            {
                return SetBindState(binding, BoundKeyState.Down, uiOnly);
            }

            return false;
        }

        private void UpBind(KeyBinding binding)
        {
            if (binding.State == BoundKeyState.Up || binding.BindingType == KeyBindingType.Toggle)
            {
                return;
            }

            SetBindState(binding, BoundKeyState.Up);
        }

        private bool SetBindState(KeyBinding binding, BoundKeyState state, bool uiOnly = false)
        {
            if (binding.BindingType == KeyBindingType.Command && state == BoundKeyState.Down)
            {
                _console.ExecuteCommand(binding.FunctionCommand);
                return true;
            }

            // christ this crap *is* re-entrant thanks to PlacementManager and
            // I honestly have no idea what the best solution here is.
            // note from the future: context switches won't cause re-entrancy anymore because InputContextContainer defers context switches
            DebugTools.Assert(!_currentlyFindingViewport, "Re-entrant key events??");

            try
            {
                // This is terrible but anyways.
                // This flag keeps track of "did a viewport fire the key up for us" so we know we don't do it again.
                _currentlyFindingViewport = true;
                // And this stops context switches from causing crashes
                Contexts.DeferringEnabled = true;

                binding.State = state;

                var eventArgs = new BoundKeyEventArgs(binding.Function, binding.State,
                    MouseScreenPosition, binding.CanFocus);

                // UI returns true here into blockPass if it wants to prevent us from giving input events
                // to the viewport, but doesn't want it hard-handled so we keep processing possible key actions.
                var blockPass = UIKeyBindStateChanged?.Invoke(eventArgs);
                if ((state == BoundKeyState.Up || (!(blockPass == true || eventArgs.Handled) && !uiOnly))
                    && _currentlyFindingViewport)
                {
                    ViewportKeyEvent(null, eventArgs);
                }

                return eventArgs.Handled;
            }
            finally
            {
                _currentlyFindingViewport = false;
                Contexts.DeferringEnabled = false;
            }
        }

        public void ViewportKeyEvent(Control? viewport, BoundKeyEventArgs eventArgs)
        {
            _currentlyFindingViewport = false;
            Contexts.DeferringEnabled = false;

            var cmd = GetInputCommand(eventArgs.Function);
            // TODO: Allow input commands to still get forwarded to server if necessary.
            if (cmd != null)
            {
                // Out-of-simulation input event
                if (eventArgs.State == BoundKeyState.Up)
                {
                    cmd.Disabled(null);
                }
                else
                {
                    cmd.Enabled(null);
                }
            }
            else
            {
                var viewportEventArgs = new ViewportBoundKeyEventArgs(eventArgs, viewport);
                // In-simulation input event (through content to InputSystem)
                KeyBindStateChanged?.Invoke(viewportEventArgs);

                if (viewportEventArgs.KeyEventArgs.Handled)
                {
                    eventArgs.Handle();
                }
            }
        }

        private bool PackedMatchesPressedState(PackedKeyCombo packed)
        {
            var (baseKey, mod1, mod2, mod3) = packed;

            if (!_keysPressed[(int) baseKey]) return false;
            if (mod1 != Key.Unknown && !_keysPressed[(int) mod1]) return false;
            if (mod2 != Key.Unknown && !_keysPressed[(int) mod2]) return false;
            if (mod3 != Key.Unknown && !_keysPressed[(int) mod3]) return false;

            return true;
        }

        private static bool PackedContainsKey(PackedKeyCombo packed, Key key)
        {
            var (baseKey, mod1, mod2, mod3) = packed;

            if (baseKey == key) return true;
            if (mod1 != Key.Unknown && mod1 == key) return true;
            if (mod2 != Key.Unknown && mod2 == key) return true;
            if (mod3 != Key.Unknown && mod3 == key) return true;

            return false;
        }

        private static bool PackedIsSubPattern(PackedKeyCombo packedCombo, PackedKeyCombo subPackedCombo)
        {
            for (var i = 0; i < 32; i += 8)
            {
                var key = (Key) ((subPackedCombo.Packed >> i) & 0b_1111_1111);
                if (key != Key.Unknown && !PackedContainsKey(packedCombo, key))
                {
                    return false;
                }
            }

            return true;
        }

        private void LoadKeyFile(ResourcePath file, bool userData)
        {
            TextReader reader;
            if (userData)
            {
                reader = _resourceMan.UserData.OpenText(file);
            }
            else
            {
                reader = _resourceMan.ContentFileReadText(file);
            }

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            var mapping = (YamlMappingNode) yamlStream.Documents[0].RootNode;

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            var robustMapping = mapping.ToDataNode() as MappingDataNode;
            if (robustMapping == null) throw new InvalidOperationException();

            if (robustMapping.TryGet("binds", out var BaseKeyRegsNode))
            {
                var baseKeyRegs = serializationManager.Read<KeyBindingRegistration[]>(BaseKeyRegsNode);

                foreach (var reg in baseKeyRegs)
                {
                    if (reg.Type != KeyBindingType.Command && !NetworkBindMap.FunctionExists(reg.Function.FunctionName))
                    {
                        Logger.ErrorS("input", "Key function in {0} does not exist: '{1}'", file,
                            reg.Function);
                        continue;
                    }

                    if (!userData)
                    {
                        _defaultRegistrations.Add(reg);

                        if (_modifiedKeyFunctions.Contains(reg.Function))
                        {
                            // Don't read key functions from preset files that have been modified.
                            // So that we don't bulldoze a user's saved preferences.
                            continue;
                        }
                    }

                    RegisterBinding(reg, markModified: userData);
                }
            }

            if (userData && robustMapping.TryGet("leaveEmpty", out var node))
            {
                var leaveEmpty = serializationManager.Read<BoundKeyFunction[]>(node);

                if (leaveEmpty.Length > 0)
                {
                    // Adding to _modifiedKeyFunctions means that these keybinds won't be loaded from the base file.
                    // Because they've been explicitly cleared.
                    _modifiedKeyFunctions.UnionWith(leaveEmpty);
                }
            }
        }

        /// <inheritdoc />
        public IKeyBinding RegisterBinding(BoundKeyFunction function, KeyBindingType bindingType,
            Key baseKey, Key? mod1, Key? mod2, Key? mod3)
        {
            var binding = new KeyBinding(this, function.FunctionName, bindingType, baseKey, false, false, false,
                0, mod1 ?? Key.Unknown, mod2 ?? Key.Unknown, mod3 ?? Key.Unknown);

            RegisterBinding(binding);

            return binding;
        }

        public IKeyBinding RegisterBinding(string function, KeyBindingType bindingType,
            Key baseKey, Key? mod1, Key? mod2, Key? mod3)
        {
            var binding = new KeyBinding(this, function, bindingType, baseKey, false, false, false,
                0, mod1 ?? Key.Unknown, mod2 ?? Key.Unknown, mod3 ?? Key.Unknown);

            RegisterBinding(binding);

            return binding;
        }

        public IKeyBinding RegisterBinding(in KeyBindingRegistration reg, bool markModified = true)
        {
            var binding = new KeyBinding(this, reg.Function.FunctionName, reg.Type, reg.BaseKey, reg.CanFocus, reg.CanRepeat,
                reg.AllowSubCombs, reg.Priority, reg.Mod1, reg.Mod2, reg.Mod3);

            RegisterBinding(binding, markModified);

            return binding;
        }

        public void RemoveBinding(IKeyBinding binding, bool markModified = true)
        {
            var bindings = _bindingsByFunction[binding.Function];
            var cast = (KeyBinding) binding;
            if (!bindings.Remove(cast))
            {
                // Keybind does not exist.
                return;
            }

            if (markModified)
            {
                _modifiedKeyFunctions.Add(binding.Function);
            }

            _bindings.Remove(cast);
            OnKeyBindingRemoved?.Invoke(binding);
        }

        public void InputModeChanged() => OnInputModeChanged?.Invoke();

        private void RegisterBinding(KeyBinding binding, bool markModified = true)
        {
            // we sort larger combos first so they take priority over smaller (single key) combos,
            // so they get processed first in KeyDown and such.
            var pos = _bindings.BinarySearch(binding, KeyBinding.ProcessPriorityComparer);
            if (pos < 0)
            {
                pos = ~pos;
            }

            if (markModified)
            {
                _modifiedKeyFunctions.Add(binding.Function);
            }

            _bindings.Insert(pos, binding);
            _bindingsByFunction.GetOrNew(binding.Function).Add(binding);
            OnKeyBindingAdded?.Invoke(binding);
        }

        /// <inheritdoc />
        public IKeyBinding GetKeyBinding(BoundKeyFunction function)
        {
            if (TryGetKeyBinding(function, out var binding))
            {
                return binding;
            }

            throw new KeyNotFoundException($"No keys are bound for function '{function}'");
        }

        public IReadOnlyList<IKeyBinding> GetKeyBindings(BoundKeyFunction function)
        {
            return _bindingsByFunction.GetOrNew(function);
        }

        public void ResetBindingsFor(BoundKeyFunction function)
        {
            foreach (var binding in GetKeyBindings(function).ToArray())
            {
                RemoveBinding(binding);
            }

            // Mark as unmodified.
            _modifiedKeyFunctions.Remove(function);

            foreach (var defaultBinding in _defaultRegistrations.Where(p => p.Function == function))
            {
                RegisterBinding(defaultBinding, markModified: false);
            }
        }

        public void ResetAllBindings()
        {
            foreach (var modified in _modifiedKeyFunctions.ToArray())
            {
                ResetBindingsFor(modified);
            }
        }

        public bool IsKeyFunctionModified(BoundKeyFunction function)
        {
            return _modifiedKeyFunctions.Contains(function);
        }

        public bool IsKeyDown(Key key)
        {
            return _keysPressed[(int) key];
        }

        /// <inheritdoc />
        public bool TryGetKeyBinding(BoundKeyFunction function, [NotNullWhen(true)] out IKeyBinding? binding)
        {
            if (!_bindingsByFunction.TryGetValue(function, out var bindings))
            {
                binding = null;
                return false;
            }

            binding = bindings.FirstOrDefault();
            return binding != null;
        }

        /// <inheritdoc />
        public InputCmdHandler? GetInputCommand(BoundKeyFunction function)
        {
            if (_commands.TryGetValue(function, out var val))
            {
                return val;
            }

            return null;
        }

        /// <inheritdoc />
        public void SetInputCommand(BoundKeyFunction function, InputCmdHandler? cmdHandler)
        {
            if (cmdHandler == null)
            {
                _commands.Remove(function);
            }
            else
            {
                _commands[function] = cmdHandler;
            }
        }

        [DebuggerDisplay("KeyBinding {" + nameof(Function) + "}")]
        private sealed class KeyBinding : IKeyBinding
        {
            private readonly InputManager _inputManager;

            [ViewVariables] public BoundKeyState State { get; set; }
            public PackedKeyCombo PackedKeyCombo { get; }
            [ViewVariables] public BoundKeyFunction Function { get; }
            [ViewVariables] public string FunctionCommand => Function.FunctionName;
            [ViewVariables] public KeyBindingType BindingType { get; }

            [ViewVariables] public Key BaseKey => PackedKeyCombo.BaseKey;
            [ViewVariables] public Key Mod1 => PackedKeyCombo.Mod1;
            [ViewVariables] public Key Mod2 => PackedKeyCombo.Mod2;
            [ViewVariables] public Key Mod3 => PackedKeyCombo.Mod3;

            /// <summary>
            ///     Whether the BoundKey can change the focused control.
            /// </summary>
            [ViewVariables]
            public bool CanFocus { get; internal set; }

            /// <summary>
            ///     Whether the BoundKey still triggers while held down.
            /// </summary>
            [ViewVariables]
            public bool CanRepeat { get; internal set; }

            /// <summary>
            ///     Whether the Bound Key Combination allows Sub Combinations of it to trigger.
            /// </summary>
            [ViewVariables]
            public bool AllowSubCombs { get; internal set; }

            [ViewVariables] public int Priority { get; internal set; }

            public KeyBinding(
                InputManager inputManager,
                string function,
                KeyBindingType bindingType,
                Key baseKey,
                bool canFocus, bool canRepeat, bool allowSubCombs, int priority, Key mod1 = Key.Unknown,
                Key mod2 = Key.Unknown,
                Key mod3 = Key.Unknown)
            {
                Function = function;
                BindingType = bindingType;
                CanFocus = canFocus;
                CanRepeat = canRepeat;
                AllowSubCombs = allowSubCombs;
                Priority = priority;
                _inputManager = inputManager;

                PackedKeyCombo = new PackedKeyCombo(baseKey, mod1, mod2, mod3);
            }

            public string GetKeyString()
            {
                var (baseKey, mod1, mod2, mod3) = PackedKeyCombo;

                var sb = new StringBuilder();

                if (mod3 != Key.Unknown)
                {
                    sb.AppendFormat("{0}+", _inputManager.GetKeyName(mod3));
                }

                if (mod2 != Key.Unknown)
                {
                    sb.AppendFormat("{0}+", _inputManager.GetKeyName(mod2));
                }

                if (mod1 != Key.Unknown)
                {
                    sb.AppendFormat("{0}+", _inputManager.GetKeyName(mod1));
                }

                sb.Append(_inputManager.GetKeyName(baseKey));

                return sb.ToString();
            }

            private sealed class ProcessPriorityRelationalComparer : IComparer<KeyBinding>
            {
                public int Compare(KeyBinding? x, KeyBinding? y)
                {
                    if (ReferenceEquals(x, y)) return 0;
                    if (ReferenceEquals(null, y)) return 1;
                    if (ReferenceEquals(null, x)) return -1;
                    var cmp = y.PackedKeyCombo.Packed.CompareTo(x.PackedKeyCombo.Packed);
                    // Higher priority is first in the list so gets to go first.
                    return cmp != 0 ? cmp : y.Priority.CompareTo(x.Priority);
                }
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendFormat("{0}: {1}", Function, BaseKey);
                if (Mod1 != Key.Unknown)
                {
                    sb.AppendFormat("+{0}", Mod1);
                    if (Mod2 != Key.Unknown)
                    {
                        sb.AppendFormat("+{0}", Mod2);
                        if (Mod3 != Key.Unknown)
                        {
                            sb.AppendFormat("+{0}", Mod3);
                        }
                    }
                }

                return sb.ToString();
            }

            public static IComparer<KeyBinding> ProcessPriorityComparer { get; } =
                new ProcessPriorityRelationalComparer();
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct PackedKeyCombo : IEquatable<PackedKeyCombo>
        {
            [FieldOffset(0)] public readonly int Packed;
            [FieldOffset(0)] public readonly Key Mod3;
            [FieldOffset(1)] public readonly Key Mod2;
            [FieldOffset(2)] public readonly Key Mod1;
            [FieldOffset(3)] public readonly Key BaseKey;

            public PackedKeyCombo(Key baseKey,
                Key mod1 = Key.Unknown,
                Key mod2 = Key.Unknown,
                Key mod3 = Key.Unknown)
            {
                if (baseKey == Key.Unknown)
                    throw new ArgumentOutOfRangeException(nameof(baseKey), baseKey, "Cannot bind Unknown key.");

                // Modifiers are sorted so that the higher key values are lower in the integer bytes.
                // Unknown is zero so at the very "top".
                // More modifiers thus takes precedent with that sort in RegisterBinding,
                // and order only matters for amount of modifiers, not the modifiers themselves,
                // Use a simplistic bubble sort to sort the key modifiers.
                if (mod1 < mod2) (mod1, mod2) = (mod2, mod1);
                if (mod2 < mod3) (mod2, mod3) = (mod3, mod2);
                if (mod1 < mod2) (mod1, mod2) = (mod2, mod1);

                // Working around the fact that C# is not aware of Explicit layout
                // and requires all struct fields be initialized.
                Packed = default;

                BaseKey = baseKey;
                Mod1 = mod1;
                Mod2 = mod2;
                Mod3 = mod3;
            }

            public void Deconstruct(out Key baseKey, out Key mod1, out Key mod2, out Key mod3)
            {
                baseKey = BaseKey;
                mod1 = Mod1;
                mod2 = Mod2;
                mod3 = Mod3;
            }

            public bool Equals(PackedKeyCombo other)
            {
                return Packed == other.Packed;
            }

            public override bool Equals(object? obj)
            {
                return obj is PackedKeyCombo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Packed;
            }

            public static bool operator ==(PackedKeyCombo left, PackedKeyCombo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(PackedKeyCombo left, PackedKeyCombo right)
            {
                return !left.Equals(right);
            }
        }
    }

    public enum KeyBindingType : byte
    {
        Unknown = 0,
        State,
        Toggle,
        /// <summary>
        /// This keybind does not execute a real key function but instead causes a console command to be executed.
        /// </summary>
        Command,
    }

    public enum CommandState : byte
    {
        Unknown = 0,
        Enabled,
        Disabled,
    }

    [UsedImplicitly]
    internal sealed class BindCommand : IConsoleCommand
    {
        public string Command => "bind";
        public string Description => "Binds an input key to an input command.";
        public string Help => "bind <KeyName> <BindMode> <InputCommand>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine("Too few arguments.");
                return;
            }

            if (args.Length > 3)
            {
                shell.WriteLine("Too many arguments.");
                return;
            }

            var keyName = args[0];

            if (!Enum.TryParse(typeof(Key), keyName, true, out var keyIdObj))
            {
                shell.WriteLine($"Key '{keyName}' is unrecognized.");
                return;
            }

            var keyId = (Key) keyIdObj!;

            if (!Enum.TryParse(typeof(KeyBindingType), args[1], true, out var keyModeObj))
            {
                shell.WriteLine($"BindMode '{args[1]}' is unrecognized.");
                return;
            }

            var keyMode = (KeyBindingType) keyModeObj!;

            var inputCommand = args[2];

            var inputMan = IoCManager.Resolve<IInputManager>();

            var registration = new KeyBindingRegistration
            {
                Function = inputCommand,
                BaseKey = keyId,
                Type = keyMode
            };

            inputMan.RegisterBinding(registration);
        }
    }

    [UsedImplicitly]
    internal sealed class SaveBindCommand : IConsoleCommand
    {
        public string Command => "svbind";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IInputManager>()
                .SaveToUserData();
        }
    }
}
