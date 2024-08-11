using System;

#if !TOOLS
namespace Robust.Client.UserInterface.XAML.Proxy {
    public sealed class XamlProxyManager: IXamlProxyManager
    {
        public void Initialize()
        {
        }

        public bool CanSetImplementation(string fileName)
        {
            return false;
        }

        public void SetImplementation(string fileName, string fileContent)
        {
        }

        public bool Populate(Type t, object o)
        {
            return false;
        }
    }
}
#endif


