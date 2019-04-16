namespace Robust.Client.Interfaces.UserInterface
{
    public interface IClipboardManager
    {
        bool Available { get; }
        string NotAvailableReason { get; }

        string GetText();
        void SetText(string text);
    }
}
