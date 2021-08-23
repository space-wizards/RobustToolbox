using System.Text;
using Robust.Client.Input;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface
{
    internal interface IRawInputControl
    {
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
