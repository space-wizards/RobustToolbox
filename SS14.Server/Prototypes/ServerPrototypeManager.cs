using SS14.Shared.Prototypes;

namespace SS14.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        public ServerPrototypeManager() : base()
        {
            RegisterIgnore("shader");
        }
    }
}
