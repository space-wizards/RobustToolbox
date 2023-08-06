using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Server.Console
{
    internal sealed class ConGroupController : IConGroupController, IPermissionController
    {
        public IConGroupControllerImplementation? Implementation { get; set; }

        public bool CanCommand(IPlayerSession session, string cmdName)
        {
            return Implementation?.CanCommand(session, cmdName) ?? false;
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

        public bool CanAdminReloadPrototypes(IPlayerSession session)
        {
            return Implementation?.CanAdminReloadPrototypes(session) ?? false;
        }

        public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error)
        {
            error = null;
            return Implementation?.CheckInvokable(command, user, out error) ?? false;
        }
    }
}
