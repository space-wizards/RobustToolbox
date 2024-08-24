using System;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.XAML.Proxy
{
    /// <summary>
    /// Reexport the Populate method of <see cref="IXamlProxyManager"/> from a
    /// non-generic concrete type.
    /// </summary>
    /// <remarks>
    /// Specializing an interface to use an argument of generic type doesn't work
    /// super well in our sandbox, for whatever reason.
    ///
    /// This type doesn't have any conditional compilation associated with it --
    /// in practice, its method is stubbed when <see cref="IXamlProxyManager" />'s
    /// Populate method is stubbed.
    /// </remarks>
    public sealed class XamlProxyHelper
    {
        [Dependency] private IXamlProxyManager _xamlProxyManager = default!;

        public bool Populate(Type t, object o)
        {
            return _xamlProxyManager.Populate(t, o);
        }
    }

}
