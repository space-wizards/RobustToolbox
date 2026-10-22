using System.Threading.Tasks;

namespace Robust.Client.UserInterface
{
    [NotContentImplementable]
    public interface IClipboardManager
    {
        Task<string> GetText();
        void SetText(string text);
    }
}
