using Robust.Client.UserInterface;

namespace Robust.Client.Debugging.XAMLUI
{
#if DEBUG
    public interface IXamlUiHotReloadManager
    {
        bool IsReloaded<T>(T control) where T : Control;


    }

    public class XamlUiHotreloadManager : IXamlUiHotReloadManager
    {

    }
#endif
}
