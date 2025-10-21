namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This service locates the SS14 source tree and watches for changes to its xaml files.
/// </summary>
/// <remarks>
/// It then reloads them instantly.
///
/// It depends on <see cref="IXamlProxyManager"/> and is stubbed on non-TOOLS builds.
/// </remarks>
interface IXamlHotReloadManager
{
    /// <summary>
    /// Initialize the hot reload manager.
    /// </summary>
    /// <remarks>
    /// You can't do anything with this once it's started, including turn it off.
    /// </remarks>
    void Initialize();
}
