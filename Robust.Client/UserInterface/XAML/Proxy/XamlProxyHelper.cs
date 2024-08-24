using System;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.XAML.Proxy;

internal sealed class XamlProxyHelper: IXamlProxyHelper
{
    [Dependency] private IXamlProxyManager _xamlProxyManager = default!;

    public bool Populate(Type t, object o)
    {
        return _xamlProxyManager.Populate(t, o);
    }
}
