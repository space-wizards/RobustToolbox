using System;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// The stub implementation of <see cref="IXamlProxyManager"/>.
/// </summary>
public sealed class XamlProxyManagerStub: IXamlProxyManager
{
    /// <summary>
    /// Do nothing.
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    /// Return false. Nothing is ever interested in a Xaml content update when
    /// hot reloading is off.
    /// </summary>
    /// <param name="fileName">the filename</param>
    /// <returns>false</returns>
    public bool CanSetImplementation(string fileName)
    {
        return false;
    }

    /// <summary>
    /// Do nothing. A hot reload will always silently fail if hot reloading is off.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="fileContent"></param>
    public void SetImplementation(string fileName, string fileContent)
    {
    }

    /// <summary>
    /// Return false.
    /// </summary>
    /// <remarks>
    /// There will never be a JIT-ed implementation of Populate if hot reloading is off.
    /// </remarks>
    /// <param name="t">the static type of <paramref name="o" /></param>
    /// <param name="o">an instance of <paramref name="t" /> or a subclass</param>
    /// <returns>false</returns>
    public bool Populate(Type t, object o)
    {
        return false;
    }
}
