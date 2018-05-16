using System;
using System.Collections.Generic;
using System.IO;
using SS14.Client.Interfaces.Input;
using SS14.Shared.Input;
using SS14.Shared.Interfaces;
using SS14.Shared.IoC;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.Input
{
    public class KeyParsing : IInputManager
    {
        [Dependency]
        private readonly IResourceManager _resourceMan;

        private List<KeyBinding> _bindings = new List<KeyBinding>();

        private bool[] _keysPressed = new bool[256];
        private bool[] _keysProcessed = new bool[256];

        public event EventHandler<InputCommand> Commands;

        public void RegisterBinding(KeyBinding binding)
        {
            //TODO: Assert there are no duplicate binding combos

            _bindings.Add(binding);

            // reversed a,b for descending order
            // we sort larger combos first so they take priority over smaller (single key) combos
            _bindings.Sort((a, b) => b.PackedKeyCombo.CompareTo(a.PackedKeyCombo));
        }

        public void KeyDown(KeyEventArgs args)
        {
            var internalKey = SfmlToInternal(args.Key);
            _keysPressed[internalKey] = true;

            int matchedCombo = 0;

            // bindings are ordered with larger combos before single key bindings so combos have priority.
            foreach (var binding in _bindings)
            {
                if (PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    // this statement *should* always be true first
                    if (PackedContainsKey(binding.PackedKeyCombo, internalKey)) // first key match becomes pressed
                    {
                        matchedCombo = binding.PackedKeyCombo;
                        // enable binding because we just finished its combo
                        var newState = binding.BindingType == KeyBindingType.Toggle ? CommandState.Toggled : CommandState.Enabled;
                        Commands?.Invoke(this, new InputCommand(binding.Function, newState)); // activate binding
                    }
                    else if (PackedIsSubPattern(matchedCombo, binding.PackedKeyCombo))
                    {
                        // kill any lower level matches
                        Commands?.Invoke(this, new InputCommand(binding.Function, CommandState.Disabled)); // deactivate any other binding
                    }
                }
            }
        }

        public void KeyUp(KeyEventArgs args)
        {
            var internalKey = SfmlToInternal(args.Key);
            foreach (var binding in _bindings)
            {
                if (PackedContainsKey(binding.PackedKeyCombo, internalKey) && PackedMatchesPressedState(binding.PackedKeyCombo))
                {
                    if (binding.BindingType == KeyBindingType.State)
                    {
                        Commands?.Invoke(this, new InputCommand(binding.Function, CommandState.Disabled));
                    }
                }
            }

            _keysPressed[internalKey] = false;
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

        private static bool PackedIsSubPattern(int packedCombo, int subPackedCombo)
        {
            if (packedCombo == subPackedCombo)
                return true;

            if ((subPackedCombo & 0x00FFFFFF) == subPackedCombo)
                return true;

            if ((subPackedCombo & 0x0000FFFF) == subPackedCombo)
                return true;

            if ((subPackedCombo & 0x000000FF) == subPackedCombo)
                return true;

            return false;
        }

        public static byte SfmlToInternal(Keyboard.Key key)
        {
            return (byte)((int)key + 1);
        }

        public static Keyboard.Key InternalToSfml(byte key)
        {
            return (Keyboard.Key)(key - 1);
        }

        public void LoadKeyFile(string yamlFile)
        {

        }

        public void SaveKeyFile(string yamlPath)
        {
            var root = new YamlMappingNode();

            // fill up root to the brim

            var document = new YamlDocument(root);

            var rootPath = _resourceMan.ConfigDirectory;
            var path = Path.Combine(rootPath, "./", yamlPath);
            var fullPath = Path.GetFullPath(path);

            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir ?? throw new InvalidOperationException("Full YamlPath was null."));

            using (var writer = new StreamWriter(fullPath))
            {
                var stream = new YamlStream();

                stream.Add(document);
                stream.Save(writer);
            }
        }
    }

    public class KeyBinding
    {
        public int PackedKeyCombo { get; }
        public BoundKeyFunctions Function { get; }
        public KeyBindingType BindingType { get; }

        public KeyBinding(BoundKeyFunctions function, KeyBindingType bindingType, Keyboard.Key baseKey, Keyboard.Key mod1 = Keyboard.Key.Unknown, Keyboard.Key mod2 = Keyboard.Key.Unknown, Keyboard.Key mod3 = Keyboard.Key.Unknown)
        {
            Function = function;
            BindingType = bindingType;

            PackedKeyCombo = PackKeyCombo(baseKey, mod1, mod2, mod3);
        }

        public static int PackKeyCombo(Keyboard.Key baseKey, Keyboard.Key mod1 = Keyboard.Key.Unknown, Keyboard.Key mod2 = Keyboard.Key.Unknown, Keyboard.Key mod3 = Keyboard.Key.Unknown)
        {
            if (baseKey == Keyboard.Key.Unknown)
                throw new ArgumentOutOfRangeException(nameof(baseKey), baseKey, "Cannot bind Unknown key.");

            //pack key combo
            var combo = 0x00000000;
            combo |= KeyParsing.SfmlToInternal(baseKey);

            if (mod1 != Keyboard.Key.Unknown)
                combo |= KeyParsing.SfmlToInternal(mod1) << 8;
            if (mod2 != Keyboard.Key.Unknown)
                combo |= KeyParsing.SfmlToInternal(mod2) << 16;
            if (mod3 != Keyboard.Key.Unknown)
                combo |= KeyParsing.SfmlToInternal(mod3) << 24;

            return combo;
        }
    }

    public class InputCommand : EventArgs
    {
        public BoundKeyFunctions Function { get; }
        public CommandState State { get; }

        public InputCommand(BoundKeyFunctions function, CommandState state)
        {
            Function = function;
            State = state;
        }
    }

    public enum KeyBindingType
    {
        Unknown = 0,
        Toggle,
        State,
    }

    public enum CommandState
    {
        Unknown = 0,
        Toggled,
        Enabled,
        Disabled,
    }
}
