namespace Robust.Client.Interfaces.UserInterface
{
    public interface IClipboardManager
    {
        string GetText();
        void SetText(string text);
    }
}
