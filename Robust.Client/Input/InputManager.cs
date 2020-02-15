using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

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
            if (!Enabled || args.Key == Keyboard.Key.Unknown)
            {
                return;
            }

            var internalKey = KeyToInternal(args.Key);
            _keysPressed[internalKey] = true;

            var matchedCombo = 0;

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
                    if ((matchedCombo == 0 || binding.PackedKeyCombo == matchedCombo) &&
                        PackedContainsKey(binding.PackedKeyCombo, internalKey))
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
            if (args.Key == Keyboard.Key.Unknown)
            {
                return;
            }

            var internalKey = KeyToInternal(args.Key);
            foreach (var binding in _bindings)
            {
                // check if our binding is even in the active context
                if (!Contexts.ActiveContext.FunctionExistsHierarchy(binding.Function))
                    continue;

                if (PackedContainsKey(binding.PackedKeyCombo, internalKey) &&
                    PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    UpBind(binding);
                }
            }

            _keysPressed[internalKey] = false;
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

        private bool PackedMatchesPressedState(int packedKeyCombo)
        {
            var key = (byte) (packedKeyCombo & 0x000000FF);
            if (!_keysPressed[key]) return false;

            key = (byte) ((packedKeyCombo & 0x0000FF00) >> 8);
            if (key != 0x00 && !_keysPressed[key]) return false;

            key = (byte) ((packedKeyCombo & 0x00FF0000) >> 16);
            if (key != 0x00 && !_keysPressed[key]) return false;

            key = (byte) ((packedKeyCombo & 0xFF000000) >> 24);
            if (key != 0x00 && !_keysPressed[key]) return false;

            return true;
        }

        private static bool PackedContainsKey(int packedKeyCombo, byte key)
        {
            var cKey = (byte) (packedKeyCombo & 0x000000FF);
            if (cKey == key) return true;

            cKey = (byte) ((packedKeyCombo & 0x0000FF00) >> 8);
            if (cKey != 0x00 && cKey == key) return true;

            cKey = (byte) ((packedKeyCombo & 0x00FF0000) >> 16);
            if (cKey != 0x00 && cKey == key) return true;

            cKey = (byte) ((packedKeyCombo & 0xFF000000) >> 24);
            if (cKey != 0x00 && cKey == key) return true;

            return false;
        }

        private static bool PackedIsSubPattern(int packedCombo, int subPackedCombo)
        {
            for (var i = 0; i < 32; i += 8)
            {
                var key = (byte) (subPackedCombo >> i);
                if (!PackedContainsKey(packedCombo, key))
                {
                    return false;
                }
            }

            return true;
        }

        private static byte KeyToInternal(Keyboard.Key key)
        {
            return (byte) key;
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

                var key = keyMapping.GetNode("key").AsEnum<Keyboard.Key>();

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

                var mod1 = Keyboard.Key.Unknown;
                if (keyMapping.TryGetNode("mod1", out var mod1Name))
                {
                    mod1 = mod1Name.AsEnum<Keyboard.Key>();
                }

                var mod2 = Keyboard.Key.Unknown;
                if (keyMapping.TryGetNode("mod2", out var mod2Name))
                {
                    mod2 = mod2Name.AsEnum<Keyboard.Key>();
                }

                var mod3 = Keyboard.Key.Unknown;
                if (keyMapping.TryGetNode("mod3", out var mod3Name))
                {
                    mod3 = mod3Name.AsEnum<Keyboard.Key>();
                }

                var type = keyMapping.GetNode("type").AsEnum<KeyBindingType>();

                var binding = new KeyBinding(function, type, key, canFocus, canRepeat, mod1, mod2, mod3);
                RegisterBinding(binding);
            }
        }

        public void AddClickBind()
        {
            RegisterBinding(new KeyBinding(EngineKeyFunctions.Use, KeyBindingType.State, Keyboard.Key.MouseLeft, true,
                false));
        }

        private void RegisterBinding(KeyBinding binding)
        {
            //TODO: Assert there are no duplicate binding combos
            _bindings.Add(binding);

            // reversed a,b for descending order
            // we sort larger combos first so they take priority over smaller (single key) combos
            _bindings.Sort((a, b) => b.PackedKeyCombo.CompareTo(a.PackedKeyCombo));
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
            public BoundKeyState State { get; set; }
            public int PackedKeyCombo { get; }
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

            public KeyBinding(BoundKeyFunction function,
                KeyBindingType bindingType,
                Keyboard.Key baseKey,
                bool canFocus, bool canRepeat,
                Keyboard.Key mod1 = Keyboard.Key.Unknown,
                Keyboard.Key mod2 = Keyboard.Key.Unknown,
                Keyboard.Key mod3 = Keyboard.Key.Unknown)
            {
                Function = function;
                BindingType = bindingType;
                CanFocus = canFocus;
                CanRepeat = canRepeat;

                PackedKeyCombo = PackKeyCombo(baseKey, mod1, mod2, mod3);
            }

            private static int PackKeyCombo(Keyboard.Key baseKey,
                Keyboard.Key mod1 = Keyboard.Key.Unknown,
                Keyboard.Key mod2 = Keyboard.Key.Unknown,
                Keyboard.Key mod3 = Keyboard.Key.Unknown)
            {
                if (baseKey == Keyboard.Key.Unknown)
                    throw new ArgumentOutOfRangeException(nameof(baseKey), baseKey, "Cannot bind Unknown key.");

                //pack key combo
                var combo = 0x00000000;
                combo |= KeyToInternal(baseKey);

                // Modifiers are sorted so that the higher key values are lower in the integer bytes.
                // Unknown is zero so at the very "top".
                // More modifiers thus takes precedent with that sort in register,
                // and order only matters for amount of modifiers, not the modifiers themselves,
                var int1 = KeyToInternal(mod1);
                var int2 = KeyToInternal(mod2);
                var int3 = KeyToInternal(mod3);

                // Use a simplistic bubble sort to sort the key modifiers.
                if (int1 < int2) (int1, int2) = (int2, int1);
                if (int2 < int3) (int2, int3) = (int3, int2);
                if (int1 < int2) (int1, int2) = (int2, int1);

                combo |= int1 << 8;
                combo |= int2 << 16;
                combo |= int3 << 24;

                return combo;
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
