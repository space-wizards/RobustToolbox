using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ClientInterfaces.Input;
using GorgonLibrary.InputDevices;
using SS13_Shared;

namespace ClientServices.Input
{
    public class KeyBindingManager : IKeyBindingManager
    {
        private Dictionary<KeyboardKeys, BoundKeyFunctions> _boundKeys;
        private Keyboard _keyboard;

        public Keyboard Keyboard
        {
            get { return _keyboard; }
            set
            {
                if (_keyboard != null)
                {
                    _keyboard.KeyDown -= KeyDown;
                    _keyboard.KeyUp -= KeyUp;
                }
                _keyboard = value;
                _keyboard.KeyDown += KeyDown;
                _keyboard.KeyUp += KeyUp;
            }
        }

        #region IKeyBindingManager Members

        public bool Enabled { get; set; }

        public event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        public event EventHandler<BoundKeyEventArgs> BoundKeyUp;

        public void Initialize(Keyboard keyboard)
        {
            Enabled = true;
            Keyboard = keyboard;
            LoadKeys();
        }

        #endregion

        /// <summary>
        /// Destructor -- unbinds from the keyboard input
        /// </summary>
        ~KeyBindingManager()
        {
            if (_keyboard == null) return;

            _keyboard.KeyDown -= KeyDown;
            _keyboard.KeyUp -= KeyUp;
        }

        private void KeyDown(object sender, KeyboardInputEventArgs e)
        {
            //If the key is bound, fire the BoundKeyDown event.
            if (Enabled && _boundKeys.Keys.Contains(e.Key) && BoundKeyDown != null)
                BoundKeyDown(this, new BoundKeyEventArgs(BoundKeyState.Down, _boundKeys[e.Key]));
        }

        private void KeyUp(object sender, KeyboardInputEventArgs e)
        {
            //If the key is bound, fire the BoundKeyUp event.
            if (Enabled && _boundKeys.Keys.Contains(e.Key) && BoundKeyUp != null)
                BoundKeyUp(this, new BoundKeyEventArgs(BoundKeyState.Up, _boundKeys[e.Key]));
        }

        private void LoadKeys()
        {
            var xml = new XmlDocument();
            var kb = new StreamReader("KeyBindings.xml");
            xml.Load(kb);
            XmlNodeList resources = xml.SelectNodes("KeyBindings/Binding");
            _boundKeys = new Dictionary<KeyboardKeys, BoundKeyFunctions>();
            if (resources != null)
                foreach (XmlNode node in resources.Cast<XmlNode>().Where(node => node.Attributes != null))
                {
                    _boundKeys.Add(
                        (KeyboardKeys) Enum.Parse(typeof (KeyboardKeys), node.Attributes["Key"].Value, false),
                        (BoundKeyFunctions)
                        Enum.Parse(typeof (BoundKeyFunctions), node.Attributes["Function"].Value, false));
                }
        }
    }
}