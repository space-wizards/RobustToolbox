#if !CLIENT_SCRIPTING
using System;
#endif

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private ToolshedEnvironment? _defaultEnvironment = default!;

    /// <summary>
    ///     The active permission controller, if any.
    /// </summary>
    /// <remarks>
    ///     Invocation contexts can entirely ignore this, though it's bad form to do so if they have a session on hand.
    /// </remarks>
    public IPermissionController? ActivePermissionController { get; set; }

    public ToolshedEnvironment DefaultEnvironment
    {
        get
        {
#if !CLIENT_SCRIPTING
            if (_net.IsClient)
                throw new NotImplementedException("Toolshed is not yet ready for client-side use.");
#endif
           _defaultEnvironment ??= new();
            return _defaultEnvironment;
        }
    }
}
