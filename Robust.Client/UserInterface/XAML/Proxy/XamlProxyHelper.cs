using System;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.XAML.Proxy
{
    /// <summary>
    /// Specializing an interface to use an argument of generic type doesn't work
    /// super well in our sandbox, for whatever reason.
    ///
    /// This type reexports the Populate method of IXamlProxyManager, but since its
    /// type is concrete, it works in the sandbox.
    ///
    /// This type doesn't have any conditional compilation associated with it --
    /// in practice, it inherits its behavior from IXamlProxyManager.
    /// </summary>
    public sealed class XamlProxyHelper
    {
        [Dependency] private IXamlProxyManager _xamlProxyManager = default!;

        public bool Populate(Type t, object o)
        {
            return _xamlProxyManager.Populate(t, o);
        }
    }

}
