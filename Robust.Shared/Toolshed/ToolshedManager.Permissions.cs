namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    /// <summary>
    ///     The active permission controller, if any.
    /// </summary>
    /// <remarks>
    ///     Invocation contexts can entirely ignore this, though it's bad form to do so if they have a session on hand.
    /// </remarks>
    public IPermissionController? ActivePermissionController { get; set; }
}
