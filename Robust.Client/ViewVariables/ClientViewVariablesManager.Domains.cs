using System.Collections.Generic;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables;

internal sealed partial class ClientViewVariablesManager
{
    private void InitializeDomains()
    {
        RegisterDomain("guihover", ResolveGuiHoverObject, ListGuiHoverPaths);
    }

    private (ViewVariablesPath? path, string[] segments) ResolveGuiHoverObject(string path)
    {
        var segments = path.Split('/');

        return (_userInterfaceManager.CurrentlyHovered != null
            ? new ViewVariablesInstancePath(_userInterfaceManager.CurrentlyHovered)
            : null, segments);
    }

    private IEnumerable<string>? ListGuiHoverPaths(string[] segments)
    {
        return null;
    }
}
