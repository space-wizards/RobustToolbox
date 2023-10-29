using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Server.Console
{
    internal sealed class ConGroupController : IConGroupController, IPermissionController
    {
        public IConGroupControllerImplementation? Implementation { get; set; }

        public bool CanCommand(ICommonSession session, string cmdName)
        {
            return Implementation?.CanCommand(session, cmdName) ?? false;
        }

        public bool CanAdminPlace(ICommonSession session)
        {
            return Implementation?.CanAdminPlace(session) ?? false;
        }

        public bool CanScript(ICommonSession session)
        {
            return Implementation?.CanScript(session) ?? false;
        }

        public bool CanAdminMenu(ICommonSession session)
        {
            return Implementation?.CanAdminMenu(session) ?? false;
        }

        public bool CanAdminReloadPrototypes(ICommonSession session)
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
