using Robust.Client.Input;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface
{
    /// <summary>
    /// Allows a control to listen for raw keyboard events. This allows bypassing the input binding system.
    /// </summary>
    /// <remarks>
    /// Raw key events are raised *after* keybindings and focusing has been calculated,
    /// but before key bind events are actually raised.
    /// This is necessary to allow UI system stuff to actually work correctly.
    /// </remarks>
    internal interface IRawInputControl
    {
        /// <param name="guiRawEvent"></param>
        /// <returns>If true: all further key bind events should be blocked.</returns>
        bool RawKeyEvent(in GuiRawKeyEvent guiRawEvent) => false;
        // bool RawCharEvent(in GuiRawCharEvent guiRawCharEvent) => false;
    }

    /*
    internal struct GuiRawCharEvent
    {
        // public readonly
        public readonly RawKeyAction Action;
        public readonly Vector2i MouseRelative;
        public readonly Rune Char;
    }
    */

    internal readonly struct GuiRawKeyEvent
    {
        public readonly Keyboard.Key Key;
        public readonly int ScanCode;
        public readonly RawKeyAction Action;
        public readonly Vector2i MouseRelative;

        public GuiRawKeyEvent(Keyboard.Key key, int scanCode, RawKeyAction action, Vector2i mouseRelative)
        {
            Key = key;
            ScanCode = scanCode;
            Action = action;
            MouseRelative = mouseRelative;
        }
    }

    public enum RawKeyAction : byte
    {
        Down,
        Repeat,
        Up
    }
}
