using Robust.Server.Interfaces.Player;

namespace Robust.Server.Console
{
    internal sealed class ConGroupController : IConGroupController
    {
        public IConGroupControllerImplementation? Implementation { get; set; }

        public bool CanCommand(IPlayerSession session, string cmdName)
        {
            return Implementation?.CanCommand(session, cmdName) ?? false;
        }

        public bool CanViewVar(IPlayerSession session)
        {
            return Implementation?.CanViewVar(session) ?? false;
        }

        public bool CanAdminPlace(IPlayerSession session)
        {
            return Implementation?.CanAdminPlace(session) ?? false;
        }

        public bool CanScript(IPlayerSession session)
        {
            return Implementation?.CanScript(session) ?? false;
        }

        public bool CanAdminMenu(IPlayerSession session)
        {
            return Implementation?.CanAdminMenu(session) ?? false;
        }
    }
}
