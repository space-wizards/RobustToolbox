namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This service locates the SS14 source tree and watches for changes to its Xaml files.
///
/// It then reloads them instantly.
///
/// It depends on XamlProxyManager and is only available on TOOLS builds.
/// </summary>
interface IXamlHotReloadManager
{
    /// <summary>
    /// Initialize the hot reload manager.
    ///
    /// You can't do anything with this once it's started, including turn it off.
    /// </summary>
    void Initialize();
}
