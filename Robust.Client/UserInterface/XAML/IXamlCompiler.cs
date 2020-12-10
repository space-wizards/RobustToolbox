using System;

namespace Robust.Client.UserInterface.XAML
{
    public interface IXamlCompiler
    {
        (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml);
    }

}
