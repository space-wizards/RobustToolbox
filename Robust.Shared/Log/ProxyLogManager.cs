namespace Robust.Shared.Log
{
    public sealed class ProxyLogManager : ILogManager
    {
        private readonly ILogManager _impl;

        public ProxyLogManager(ILogManager impl)
        {
            _impl = impl;
        }

        ISawmill ILogManager.RootSawmill => _impl.RootSawmill;

        ISawmill ILogManager.GetSawmill(string name)
        {
            return _impl.GetSawmill(name);
        }
    }
}
