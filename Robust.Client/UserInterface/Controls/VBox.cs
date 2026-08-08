namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// Convenience type to describe a vertical <see cref="BoxContainer"/>.
/// </summary>
public sealed class VBox : BoxContainer
{
    public VBox()
    {
        Orientation = LayoutOrientation.Vertical;
    }
}
