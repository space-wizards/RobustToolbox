using System;
using SKeyEventArgs = SFML.Window.KeyEventArgs;

namespace SS14.Client.Graphics.Input
{
    public class KeyEventArgs : EventArgs
    {
        /// <summary>
        /// The key that got pressed or released.
        /// </summary>
        public Keyboard.Key Key { get; }

        /// <summary>
        /// Whether the alt key (⌥ Option on MacOS) is held.
        /// </summary>
        public bool Alt { get; }

        /// <summary>
        /// Whether the control key is held.
        /// </summary>
        public bool Control { get; }

        /// <summary>
        /// Whether the shift key is held.
        /// </summary>
        public bool Shift { get; }

        /// <summary>
        /// Whether the system key (Windows key, ⌘ Command on MacOS) is held.
        /// </summary>
        public bool System { get; }

        public KeyEventArgs(Keyboard.Key key, bool alt, bool control, bool shift, bool system)
        {
            Key = key;
            Alt = alt;
            Control = control;
            Shift = shift;
            System = system;
        }

        public static explicit operator KeyEventArgs(SKeyEventArgs args)
        {
            return new KeyEventArgs(args.Code.Convert(), args.Alt, args.Control, args.Shift, args.System);
        }
    }
}
