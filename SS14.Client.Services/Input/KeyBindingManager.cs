using SFML.Window;
using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Interfaces.Input;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace SS14.Client.Services.Input
{
    public class KeyBindingManager : IKeyBindingManager
    {
        private Dictionary<Keyboard.Key, BoundKeyFunctions> _boundKeys;

        #region IKeyBindingManager Members

        public bool Enabled { get; set; }

        public event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        public event EventHandler<BoundKeyEventArgs> BoundKeyUp;

        public void Initialize()
        {
            Enabled = true;
            CluwneLib.Screen.KeyPressed += KeyDown;
            CluwneLib.Screen.KeyReleased += KeyUp;
            LoadKeys();
        }

        #endregion

        /// <summary>
        /// Destructor -- unbinds from the keyboard input
        /// </summary>
        ~KeyBindingManager()
        {
            CluwneLib.Screen.KeyPressed  -= KeyDown;
            CluwneLib.Screen.KeyReleased -= KeyUp;
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            //If the key is bound, fire the BoundKeyDown event.
            if (Enabled && _boundKeys.Keys.Contains(e.Code) && BoundKeyDown != null)
                BoundKeyDown(this, new BoundKeyEventArgs(BoundKeyState.Down, _boundKeys[e.Code]));
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            //If the key is bound, fire the BoundKeyUp event.
            if (Enabled && _boundKeys.Keys.Contains(e.Code) && BoundKeyUp != null)
                BoundKeyUp(this, new BoundKeyEventArgs(BoundKeyState.Up, _boundKeys[e.Code]));
        }

        private void LoadKeys()
        {
            var xml = new XmlDocument();
            var kb = new StreamReader("KeyBindings.xml");
            xml.Load(kb);
            XmlNodeList resources = xml.SelectNodes("KeyBindings/Binding");
            _boundKeys = new Dictionary<Keyboard.Key, BoundKeyFunctions>();
            if (resources != null)
                foreach (XmlNode node in resources.Cast<XmlNode>().Where(node => node.Attributes != null))
                {
                    _boundKeys.Add(
                        (Keyboard.Key) Enum.Parse(typeof (Keyboard.Key), node.Attributes["Key"].Value, false),
                        (BoundKeyFunctions)
                        Enum.Parse(typeof (BoundKeyFunctions), node.Attributes["Function"].Value, false));
                }
        }
    }
}