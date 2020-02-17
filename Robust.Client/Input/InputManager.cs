using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Robust.Client.Interfaces.Input;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Robust.Client.Input.Keyboard;

namespace Robust.Client.Input
{
    internal class InputManager : IInputManager
    {
        public bool Enabled { get; set; } = true;

        public virtual Vector2 MouseScreenPosition => Vector2.Zero;

#pragma warning disable 649
        [Dependency] private readonly IResourceManager _resourceMan;
        [Dependency] private readonly IReflectionManager _reflectionManager;
#pragma warning restore 649

        private readonly Dictionary<BoundKeyFunction, InputCmdHandler> _commands =
            new Dictionary<BoundKeyFunction, InputCmdHandler>();

        private readonly List<KeyBinding> _bindings = new List<KeyBinding>();
        private readonly bool[] _keysPressed = new bool[256];

        /// <inheritdoc />
        public BoundKeyMap NetworkBindMap { get; private set; }

        /// <inheritdoc />
        public IInputContextContainer Contexts { get; } = new InputContextContainer();

        /// <inheritdoc />
        public event Action<BoundKeyEventArgs> UIKeyBindStateChanged;

        /// <inheritdoc />
        public event Action<BoundKeyEventArgs> KeyBindStateChanged;

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
            IKeyBinding bind;
            try
            {
                bind = GetKeyBinding(function);
            }
            catch (KeyNotFoundException)
            {
                return Loc.GetString("<not bound>");
            }

            return bind.GetKeyString();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            NetworkBindMap = new BoundKeyMap(_reflectionManager);
            NetworkBindMap.PopulateKeyFunctionsMap();

            EngineContexts.SetupContexts(Contexts);

            Contexts.ContextChanged += OnContextChanged;

            var path = new ResourcePath("/keybinds.yml");
            if (_resourceMan.ContentFileExists(path))
            {
                LoadKeyFile(path);
            }
        }

        private void OnContextChanged(object sender, ContextChangedEventArgs args)
        {
            // keyup any commands that are not in the new contexts, because it will not exist in the new context and get filtered. Luckily
            // the diff does not have to be symmetrical, otherwise instead of 'A \ B' we allocate all the things with '(A \ B) ∪ (B \ A)'
            // It should be OK to artificially keyup these, because in the future the organic keyup will be blocked (either the context
            // does not have the binding, or the double keyup check in UpBind will block it).
            foreach (var function in args.OldContext.Except(args.NewContext))
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

            _keysPressed[(int) args.Key] = true;

            PackedKeyCombo matchedCombo = default;

            // bindings are ordered with larger combos before single key bindings so combos have priority.
            foreach (var binding in _bindings)
            {
                // check if our binding is even in the active context
                if (!Contexts.ActiveContext.FunctionExistsHierarchy(binding.Function))
                    continue;

                if (PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    // this statement *should* always be true first
                    // Keep triggering keybinds of the same PackedKeyCombo until Handled or no bindings left
                    if ((matchedCombo == default || binding.PackedKeyCombo == matchedCombo) &&
                        PackedContainsKey(binding.PackedKeyCombo, args.Key))
                    {
                        matchedCombo = binding.PackedKeyCombo;

                        if (DownBind(binding))
                            break;
                    }
                    else if (PackedIsSubPattern(matchedCombo, binding.PackedKeyCombo))
                    {
                        // kill any lower level matches
                        UpBind(binding);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void KeyUp(KeyEventArgs args)
        {
            if (args.Key == Key.Unknown)
            {
                return;
            }

            foreach (var binding in _bindings)
            {
                // check if our binding is even in the active context
                if (!Contexts.ActiveContext.FunctionExistsHierarchy(binding.Function))
                    continue;

                if (PackedContainsKey(binding.PackedKeyCombo, args.Key) &&
                    PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    UpBind(binding);
                }
            }

            _keysPressed[(int) args.Key] = false;
        }

        private bool DownBind(KeyBinding binding)
        {
            if (binding.State == BoundKeyState.Down)
            {
                if (binding.BindingType == KeyBindingType.Toggle)
                {
                    return SetBindState(binding, BoundKeyState.Up);
                }
                else if (binding.CanRepeat)
                {
                    return SetBindState(binding, BoundKeyState.Down);
                }
            }
            else
            {
                return SetBindState(binding, BoundKeyState.Down);
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

        private bool SetBindState(KeyBinding binding, BoundKeyState state)
        {
            binding.State = state;

            var eventArgs = new BoundKeyEventArgs(binding.Function, binding.State,
                new ScreenCoordinates(MouseScreenPosition), binding.CanFocus);

            UIKeyBindStateChanged?.Invoke(eventArgs);
            if (state == BoundKeyState.Up || !eventArgs.Handled)
            {
                KeyBindStateChanged?.Invoke(eventArgs);

                var cmd = GetInputCommand(binding.Function);
                if (state == BoundKeyState.Up)
                {
                    cmd?.Disabled(null);
                }
                else
                {
                    cmd?.Enabled(null);
                }
            }

            return (eventArgs.Handled);
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
                var key = (Key) (subPackedCombo.Packed >> i);
                if (!PackedContainsKey(packedCombo, key))
                {
                    return false;
                }
            }

            return true;
        }

        private void LoadKeyFile(ResourcePath yamlFile)
        {
            YamlDocument document;

            using (var stream = _resourceMan.ContentFileRead(yamlFile))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);
                document = yamlStream.Documents[0];
            }

            var mapping = (YamlMappingNode) document.RootNode;
            foreach (var keyMapping in mapping.GetNode<YamlSequenceNode>("binds").Cast<YamlMappingNode>())
            {
                var function = keyMapping.GetNode("function").AsString();
                if (!NetworkBindMap.FunctionExists(function))
                {
                    Logger.ErrorS("input", "Key function in {0} does not exist: '{1}'", yamlFile, function);
                    continue;
                }

                var key = keyMapping.GetNode("key").AsEnum<Key>();

                var canFocus = false;
                if (keyMapping.TryGetNode("canFocus", out var canFocusName))
                {
                    canFocus = canFocusName.AsBool();
                }

                var canRepeat = false;
                if (keyMapping.TryGetNode("canRepeat", out var canRepeatName))
                {
                    canRepeat = canRepeatName.AsBool();
                }

                var mod1 = Key.Unknown;
                if (keyMapping.TryGetNode("mod1", out var mod1Name))
                {
                    mod1 = mod1Name.AsEnum<Key>();
                }

                var mod2 = Key.Unknown;
                if (keyMapping.TryGetNode("mod2", out var mod2Name))
                {
                    mod2 = mod2Name.AsEnum<Key>();
                }

                var mod3 = Key.Unknown;
                if (keyMapping.TryGetNode("mod3", out var mod3Name))
                {
                    mod3 = mod3Name.AsEnum<Key>();
                }

                var type = keyMapping.GetNode("type").AsEnum<KeyBindingType>();

                var binding = new KeyBinding(this, function, type, key, canFocus, canRepeat, mod1, mod2, mod3);
                RegisterBinding(binding);
            }
        }

        private void RegisterBinding(KeyBinding binding)
        {
            //TODO: Assert there are no duplicate binding combos
            _bindings.Add(binding);

            // reversed a,b for descending order
            // we sort larger combos first so they take priority over smaller (single key) combos
            _bindings.Sort((a, b) => b.PackedKeyCombo.Packed.CompareTo(a.PackedKeyCombo.Packed));
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

        /// <inheritdoc />
        public bool TryGetKeyBinding(BoundKeyFunction function, out IKeyBinding binding)
        {
            binding = _bindings.FirstOrDefault(k => k.Function == function);
            return binding != null;
        }

        /// <inheritdoc />
        public InputCmdHandler GetInputCommand(BoundKeyFunction function)
        {
            if (_commands.TryGetValue(function, out var val))
            {
                return val;
            }

            return null;
        }

        /// <inheritdoc />
        public void SetInputCommand(BoundKeyFunction function, InputCmdHandler cmdHandler)
        {
            _commands[function] = cmdHandler;
        }

        private class KeyBinding : IKeyBinding
        {
            private readonly InputManager _inputManager;

            public BoundKeyState State { get; set; }
            public PackedKeyCombo PackedKeyCombo { get; }
            public BoundKeyFunction Function { get; }
            public KeyBindingType BindingType { get; }

            /// <summary>
            ///     Whether the BoundKey can change the focused control.
            /// </summary>
            public bool CanFocus { get; internal set; }

            /// <summary>
            ///     Whether the BoundKey still triggers while held down.
            /// </summary>
            public bool CanRepeat { get; internal set; }

            public KeyBinding(InputManager inputManager, BoundKeyFunction function,
                KeyBindingType bindingType,
                Key baseKey,
                bool canFocus, bool canRepeat, Key mod1 = Key.Unknown,
                Key mod2 = Key.Unknown,
                Key mod3 = Key.Unknown)
            {
                Function = function;
                BindingType = bindingType;
                CanFocus = canFocus;
                CanRepeat = canRepeat;
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

            public override bool Equals(object obj)
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

    public enum KeyBindingType
    {
        Unknown = 0,
        State,
        Toggle,
    }

    public enum CommandState
    {
        Unknown = 0,
        Enabled,
        Disabled,
    }
}
