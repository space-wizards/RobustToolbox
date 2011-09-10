using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using System.Security;

namespace ClientInput
{
    [SecuritySafeCritical]
    public class KeyBindingManager
    {
        [SecuritySafeCritical]
        private static KeyBindingManager singleton;
        public static KeyBindingManager Singleton
        {
            [SecuritySafeCritical]
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("KeyBindingManager not initialized.", null);
                else
                    return singleton;
            }
            private set
            {
            }
        }

        private Keyboard m_keyboard;
        public Keyboard Keyboard
        {
            get
            {
                return m_keyboard;
            }
            set
            {
                if (m_keyboard != null)
                {
                    m_keyboard.KeyDown -= new KeyboardInputEvent(KeyDown);
                    m_keyboard.KeyUp -= new KeyboardInputEvent(KeyUp);
                }
                m_keyboard = value;
                m_keyboard.KeyDown += new KeyboardInputEvent(KeyDown);
                m_keyboard.KeyUp += new KeyboardInputEvent(KeyUp);
            }
        }

        private Dictionary<KeyboardKeys, KeyFunctions> BoundKeys;

        public delegate void BoundKeyEventHandler(object sender, BoundKeyEventArgs e);

        public event BoundKeyEventHandler BoundKeyDown;
        public event BoundKeyEventHandler BoundKeyUp;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public KeyBindingManager()
        {
            
        }

        ~KeyBindingManager()
        {
            if(m_keyboard != null)
            {
                m_keyboard.KeyDown -= new KeyboardInputEvent(KeyDown);
                m_keyboard.KeyUp -= new KeyboardInputEvent(KeyUp);
            }
        }

        public static void Initialize(Keyboard _keyboard)
        {
            singleton = new KeyBindingManager();
            singleton.Keyboard = _keyboard;
            singleton.LoadKeys();
           
        }
        /// <summary>
        /// Handles key down events from the gorgon keyboard object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void KeyDown(object sender, KeyboardInputEventArgs e)
        {
            //If the key is bound, fire the BoundKeyDown event.
            if (BoundKeys.Keys.Contains(e.Key))
                BoundKeyDown(this, new BoundKeyEventArgs(KeyState.Down, BoundKeys[e.Key]));
        }
        /// <summary>
        /// Handles key up events from the gorgon keyboard object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void KeyUp(object sender, KeyboardInputEventArgs e)
        {
            //If the key is bound, fire the BoundKeyUp event.
            if (BoundKeys.Keys.Contains(e.Key))
                BoundKeyUp(this, new BoundKeyEventArgs(KeyState.Down, BoundKeys[e.Key]));
        }

        public void LoadKeys()
        {
            XmlDocument xml = new XmlDocument();
            StreamReader kb = new StreamReader("KeyBindings.xml");
            xml.Load(kb);
            XmlNodeList resources = xml.SelectNodes("KeyBindings/Binding");
            BoundKeys = new Dictionary<KeyboardKeys, KeyFunctions>();
            foreach (XmlNode node in resources)
            {
                BoundKeys.Add(
                    (KeyboardKeys)Enum.Parse(typeof(KeyboardKeys), node.Attributes["Key"].Value, false), 
                    (KeyFunctions)Enum.Parse(typeof(KeyFunctions), node.Attributes["Function"].Value, false));
            }
        }

    }

    /// <summary>
    /// Key Bindings - each corresponds to a logical function ingame.
    /// </summary>
    public enum KeyFunctions
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        SwitchHands,
        Inventory,
        ShowFPS,
        Drop,
        Run,
    }

}
