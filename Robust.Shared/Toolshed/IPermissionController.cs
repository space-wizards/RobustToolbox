using JetBrains.Annotations;
using Robust.Shared.Players;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

public interface IPermissionController
{
    public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error);
}
