using System.Collections.Generic;
using Robust.Client.UserInterface;

namespace Robust.Client.Editor.Interface;

/// <summary>
/// An <see cref="EditorDocker"/> that shows its direct XAML contents only when there are no panels in it.
/// </summary>
internal sealed class PlaceholderEditorDocker : EditorDocker
{
    private readonly Control _placeHolder = new();

    public PlaceholderEditorDocker()
    {
        AddChild(_placeHolder);

        PanelAdded += OnPanelAdded;
    }

    private void OnPanelAdded(EditorPanel obj)
    {
        _placeHolder.Orphan();
    }

    protected override void LastTabRemoved()
    {
        base.LastTabRemoved();

        AddChild(_placeHolder);
    }

    public override ICollection<Control> XamlChildren => _placeHolder.Children;
}
