using System;

namespace Robust.Client.UI
{
    public interface IXamlCompiler
    {
        (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml);
    }
}
