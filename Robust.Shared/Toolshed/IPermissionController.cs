using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

public interface IPermissionController
{
    public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error);
}
