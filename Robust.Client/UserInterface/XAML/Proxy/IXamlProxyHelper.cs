using System;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// Reexport the Populate method of <see cref="IXamlProxyManager"/> and nothing else.
/// </summary>
public interface IXamlProxyHelper
{
    bool Populate(Type t, object o);
}
