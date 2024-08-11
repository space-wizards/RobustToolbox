using System;

namespace Robust.Client.UserInterface.XAML.Proxy;

public interface IXamlProxyManager
{
    void Initialize();
    bool CanSetImplementation(string fileName);
    void SetImplementation(string fileName, string fileContent);
    bool Populate(Type t, object o);
}
