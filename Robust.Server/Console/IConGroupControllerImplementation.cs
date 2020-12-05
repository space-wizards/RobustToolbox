using Robust.Server.Interfaces.Player;

namespace Robust.Server.Console
{
    public interface IConGroupControllerImplementation
    {
        bool CanCommand(IPlayerSession session, string cmdName);
        bool CanViewVar(IPlayerSession session);
        bool CanAdminPlace(IPlayerSession session);
        bool CanScript(IPlayerSession session);
        bool CanAdminMenu(IPlayerSession session);
    }
}
