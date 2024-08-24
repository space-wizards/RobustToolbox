using System;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.XAML.Proxy
{
    /// <summary>
    /// Reexport the Populate method of <see cref="IXamlProxyManager"/> from a
    /// non-generic concrete type.
    /// </summary>
    /// <remarks>
    /// (And none of the others, for security reasons.)
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
