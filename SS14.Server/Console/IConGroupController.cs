using SS14.Server.Interfaces.Player;

namespace SS14.Server.Console
{
    internal interface IConGroupController
    {
        void Initialize();

        bool CanCommand(IPlayerSession session, string cmdName);
        bool CanViewVar(IPlayerSession session);
        void SetGroup(IPlayerSession session, ConGroupIndex newGroup);
    }
}
