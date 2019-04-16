using Robust.Shared.Prototypes;

namespace Robust.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        public ServerPrototypeManager() : base()
        {
            RegisterIgnore("shader");
        }
    }
}
