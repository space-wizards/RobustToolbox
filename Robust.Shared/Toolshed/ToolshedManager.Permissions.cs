namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    public IPermissionController? ActivePermissionController { get; private set; }

    public void SetPermissionController(IPermissionController controller)
    {
        ActivePermissionController = controller;
    }
}
