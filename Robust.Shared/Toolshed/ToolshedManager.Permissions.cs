using System;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private ToolshedEnvironment? _defaultEnvironment;

    /// <summary>
    ///     The active permission controller, if any.
    /// </summary>
    /// <remarks>
    ///     Invocation contexts can entirely ignore this, though it's bad form to do so if they have a session on hand.
    /// </remarks>
    public IPermissionController? ActivePermissionController { get; set; }

    /// <summary>
    /// Check whether a command can be invoked by the given session/user.
    /// A null session implies that the command is being run by the server.
    /// </summary>
    public bool CheckInvokable(CommandSpec command, ICommonSession? session, out IConError? error)
    {
        if (ActivePermissionController is { } controller)
            return controller.CheckInvokable(command, session, out error);

        error = null;
        return true;
    }

    public ToolshedEnvironment DefaultEnvironment
    {
        get
        {
            if (!Started)
                throw new Exception("Toolshed Manager has not yet been started.");

            return _defaultEnvironment;
        }
    }
}
