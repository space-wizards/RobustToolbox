using Robust.Client.Interfaces.UserInterface;

namespace Robust.Client.UserInterface
{
    internal sealed class ClipboardManagerGodot : IClipboardManager
    {
        public bool Available => true;
        public string NotAvailableReason => "";
        public string GetText()
        {
            return Godot.OS.Clipboard;
        }

        public void SetText(string text)
        {
            Godot.OS.Clipboard = text;
        }
    }
}
