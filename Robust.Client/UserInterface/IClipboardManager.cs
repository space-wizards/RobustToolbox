using System.Threading.Tasks;

namespace Robust.Client.UserInterface
{
    public interface IClipboardManager
    {
        Task<string> GetText();
        void SetText(string text);
    }
}
