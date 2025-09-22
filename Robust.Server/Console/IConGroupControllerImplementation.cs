using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Robust.Server.Console
{
    public interface IConGroupControllerImplementation : IPermissionController
    {
        bool CanCommand(ICommonSession session, string cmdName);
        bool CanAdminPlace(ICommonSession session);
        bool CanScript(ICommonSession session);
        bool CanAdminMenu(ICommonSession session);
        bool CanAdminReloadPrototypes(ICommonSession session);
    }
}
