using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Input;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.Input
{
    public class InputManager : IInputManager
    {
        public bool Enabled { get; set; } = true;

        public virtual Vector2 MouseScreenPosition => Vector2.Zero;

        [Dependency]
        readonly IUserInterfaceManager userInterfaceManager;
        [Dependency]
        readonly IResourceManager _resourceMan;
        [Dependency]
        readonly IClientNetManager _netManager;
        [Dependency]
        readonly IReflectionManager _reflectionManager;

        private BoundKeyMap keyMap;

        private readonly Dictionary<BoundKeyFunction, InputCommand> _commands = new Dictionary<BoundKeyFunction, InputCommand>();
        private readonly List<KeyBinding> _bindings = new List<KeyBinding>();
        private bool[] _keysPressed = new bool[256];

        public event Action<BoundKeyFunction> OnKeyBindDown;
        public event Action<BoundKeyFunction> OnKeyBindUp;
        public event Action<BoundKeyEventArgs> OnKeyBindStateChanged;

        public void Initialize()
        {
            keyMap = new BoundKeyMap(_reflectionManager);
            keyMap.PopulateKeyFunctionsMap();

            LoadKeyFile(new ResourcePath("/keybinds.yml"));
            var path = new ResourcePath("/keybinds_content.yml");
            if (_resourceMan.ContentFileExists(path))
            {
                LoadKeyFile(path);
            }

            _netManager.RegisterNetMessage<MsgKeyFunctionStateChange>(MsgKeyFunctionStateChange.NAME);
        }

        public void KeyDown(KeyEventArgs args)
        {
            if (!Enabled || UIBlocked() || args.Key == Keyboard.Key.Unknown)
            {
                return;
            }

            var internalKey = KeyToInternal(args.Key);
            _keysPressed[internalKey] = true;

            int matchedCombo = 0;

            // bindings are ordered with larger combos before single key bindings so combos have priority.
            foreach (var binding in _bindings)
            {
                if (PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    // this statement *should* always be true first
                    if (matchedCombo == 0 && PackedContainsKey(binding.PackedKeyCombo, internalKey)) // first key match becomes pressed
                    {
                        matchedCombo = binding.PackedKeyCombo;

                        DownBind(binding);
                    }
                    else if (PackedIsSubPattern(matchedCombo, binding.PackedKeyCombo))
                    {
                        // kill any lower level matches
                        UpBind(binding);
                    }
                }
            }
        }

        public void KeyUp(KeyEventArgs args)
        {
            if (args.Key == Keyboard.Key.Unknown)
            {
                return;
            }
            var internalKey = KeyToInternal(args.Key);
            foreach (var binding in _bindings)
            {
                if (PackedContainsKey(binding.PackedKeyCombo, internalKey) && PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    UpBind(binding);
                }
            }

            _keysPressed[internalKey] = false;
        }

        private void DownBind(KeyBinding binding)
        {
            if (binding.State == BoundKeyState.Down)
            {
                if (binding.BindingType == KeyBindingType.Toggle)
                {
                    SetBindState(binding, BoundKeyState.Up);
                }
                else
                {
                    return;
                }
            }
            else
            {
                SetBindState(binding, BoundKeyState.Down);
            }
        }

        private void UpBind(KeyBinding binding)
        {
            if (binding.State == BoundKeyState.Up || binding.BindingType == KeyBindingType.Toggle)
            {
                return;
            }

            SetBindState(binding, BoundKeyState.Up);
        }

        private void SetBindState(KeyBinding binding, BoundKeyState state)
        {
            binding.State = state;
            var cmd = GetInputCommand(binding.Function);
            OnKeyBindStateChanged?.Invoke(new BoundKeyEventArgs(binding.Function, binding.State));
            if (state == BoundKeyState.Up)
            {
                OnKeyBindUp?.Invoke(binding.Function);
                cmd?.Disabled();
            }
            else
            {
                OnKeyBindDown?.Invoke(binding.Function);
                cmd?.Enabled();
            }

            var msg = _netManager.CreateNetMessage<MsgKeyFunctionStateChange>();
            msg.KeyFunction = keyMap.KeyFunctionID(binding.Function);
            msg.NewState = state;
            _netManager.ClientSendMessage(msg);
        }

        private bool PackedMatchesPressedState(int packedKeyCombo)
        {
            var key = (byte)(packedKeyCombo & 0x000000FF);
            if (!_keysPressed[key]) return false;

            key = (byte)((packedKeyCombo & 0x0000FF00) >> 8);
            if (key != 0x00 && !_keysPressed[key]) return false;

            key = (byte)((packedKeyCombo & 0x00FF0000) >> 16);
            if (key != 0x00 && !_keysPressed[key]) return false;

            key = (byte)((packedKeyCombo & 0xFF000000) >> 24);
            if (key != 0x00 && !_keysPressed[key]) return false;

            return true;
        }

        private static bool PackedContainsKey(int packedKeyCombo, byte key)
        {
            var cKey = (byte)(packedKeyCombo & 0x000000FF);
            if (cKey == key) return true;

            cKey = (byte)((packedKeyCombo & 0x0000FF00) >> 8);
            if (cKey != 0x00 && cKey == key) return true;

            cKey = (byte)((packedKeyCombo & 0x00FF0000) >> 16);
            if (cKey != 0x00 && cKey == key) return true;

            cKey = (byte)((packedKeyCombo & 0xFF000000) >> 24);
            if (cKey != 0x00 && cKey == key) return true;

            return false;
        }

        private static int PackedModifierCount(int packedCombo)
        {
            if ((packedCombo & 0x0000FF00) == 0x00000000) return 0;
            if ((packedCombo & 0x00FF0000) == 0x00000000) return 1;
            if ((packedCombo & 0xFF000000) == 0x00000000) return 2;
            return 3;
        }

        private static bool PackedIsSubPattern(int packedCombo, int subPackedCombo)
        {
            for (var i = 0; i < 32; i += 8)
            {
                byte key = (byte)(subPackedCombo >> i);
                if (!PackedContainsKey(packedCombo, key))
                {
                    return false;
                }
            }
            return true;
        }

        internal static byte KeyToInternal(Keyboard.Key key)
        {
            return (byte)key;
        }

        internal static Keyboard.Key InternalToKey(byte key)
        {
            return (Keyboard.Key)key;
        }

        private void LoadKeyFile(ResourcePath yamlFile)
        {
            YamlDocument document;

            using (var stream = _resourceMan.ContentFileRead(yamlFile))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);
                document = yamlStream.Documents[0];
            }

            var mapping = (YamlMappingNode)document.RootNode;
            foreach (var keyMapping in mapping.GetNode<YamlSequenceNode>("binds").Cast<YamlMappingNode>())
            {
                var function = keyMapping.GetNode("function").AsString();
                if (!keyMap.FunctionExists(function))
                {
                    Logger.ErrorS("input", "Key function in {0} does not exist: '{1}'", yamlFile, function);
                    continue;
                }
                var key = keyMapping.GetNode("key").AsEnum<Keyboard.Key>();
                var type = keyMapping.GetNode("type").AsEnum<KeyBindingType>();

                var binding = new KeyBinding(function, type, key);
                RegisterBinding(binding);
            }
        }

        private void SaveKeyFile(ResourcePath yamlPath)
        {
            throw new NotImplementedException();
        }

        private string KeyFilePath(string filename)
        {
            var rootPath = _resourceMan.ConfigDirectory;
            var path = Path.Combine(rootPath, filename);
            return Path.GetFullPath(path);
        }

        // Don't take input if we're focused on a LineEdit.
        // LineEdits don't intercept keydowns when typing properly.
        // NOTE: macOS specific!
        // https://github.com/godotengine/godot/issues/15071
        // So if we didn't do this, the DebugConsole wouldn't block movement (for example).
        private bool UIBlocked()
        {
            return userInterfaceManager.Focused is LineEdit;
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
        public InputCommand GetInputCommand(BoundKeyFunction function)
        {
            if (_commands.TryGetValue(function, out var val))
            {
                return val;
            }

            return null;
        }

        /// <inheritdoc />
        public void SetInputCommand(BoundKeyFunction function, InputCommand command)
        {
            _commands[function] = command;
        }

        class KeyBinding : IKeyBinding
        {
            public BoundKeyState State { get; set; }
            public int PackedKeyCombo { get; }
            public BoundKeyFunction Function { get; }
            public KeyBindingType BindingType { get; }

            public KeyBinding(BoundKeyFunction function,
                              KeyBindingType bindingType,
                              Keyboard.Key baseKey,
                              Keyboard.Key mod1 = Keyboard.Key.Unknown,
                              Keyboard.Key mod2 = Keyboard.Key.Unknown,
                              Keyboard.Key mod3 = Keyboard.Key.Unknown)
            {
                Function = function;
                BindingType = bindingType;

                PackedKeyCombo = PackKeyCombo(baseKey, mod1, mod2, mod3);
            }

            public static int PackKeyCombo(Keyboard.Key baseKey,
                                           Keyboard.Key mod1 = Keyboard.Key.Unknown,
                                           Keyboard.Key mod2 = Keyboard.Key.Unknown,
                                           Keyboard.Key mod3 = Keyboard.Key.Unknown)
            {
                if (baseKey == Keyboard.Key.Unknown)
                    throw new ArgumentOutOfRangeException(nameof(baseKey), baseKey, "Cannot bind Unknown key.");

                //pack key combo
                var combo = 0x00000000;
                combo |= InputManager.KeyToInternal(baseKey);

                // Modifiers are sorted so that the higher key values are lower in the integer bytes.
                // Unknown is zero so at the very "top".
                // More modifiers thus takes precedent with that sort in register,
                // and order only matters for amount of modifiers, not the modifiers themselves,
                var int1 = InputManager.KeyToInternal(mod1);
                var int2 = InputManager.KeyToInternal(mod2);
                var int3 = InputManager.KeyToInternal(mod3);

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
