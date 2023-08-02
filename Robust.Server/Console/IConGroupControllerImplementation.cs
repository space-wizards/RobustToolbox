using Robust.Server.Player;
using Robust.Shared.Toolshed;

namespace Robust.Server.Console
{
    public interface IConGroupControllerImplementation : IPermissionController
    {
        bool CanCommand(IPlayerSession session, string cmdName);
        bool CanAdminPlace(IPlayerSession session);
        bool CanScript(IPlayerSession session);
        bool CanAdminMenu(IPlayerSession session);
        bool CanAdminReloadPrototypes(IPlayerSession session);
    }
}
