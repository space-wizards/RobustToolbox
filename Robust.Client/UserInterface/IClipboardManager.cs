namespace Robust.Client.UserInterface
{
    public interface IClipboardManager
    {
        string GetText();
        void SetText(string text);
    }
}
