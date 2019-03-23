using SS14.Client.Interfaces.UserInterface;

namespace SS14.Client.UserInterface
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
