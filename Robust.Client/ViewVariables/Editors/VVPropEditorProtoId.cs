using System.Linq;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorProtoId<T> : VVPropEditor where T : class, IPrototype
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private ViewVariablesAddWindow? _addWindow;
    private LineEdit? _lineEdit;

    protected override Control MakeUI(object? value)
    {
        // ID LineEdit
        _lineEdit = new LineEdit
        {
            Text = (ProtoId<T>)(value ?? ""),
            PlaceHolder = _loc.GetString("vv-protoid-id-placeholder"),
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            _lineEdit.OnTextEntered += e =>
            {
                SetValue(e.Text);
            };
        }

        // Select button
        var selectButton = new Button
        {
            Text = _loc.GetString("vv-protoid-select-button-label"),
            Disabled = ReadOnly,
        };
        selectButton.OnPressed += OnListButtonPressed;

        // Container
        var hBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Children =
            {
                _lineEdit,
                selectButton,
            }
        };

        return hBox;
    }

    private void OnListButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _addWindow?.Close();

        var list = _protoManager.EnumeratePrototypes<T>().Select(p => p.ID);

        _addWindow = new ViewVariablesAddWindow(list, _loc.GetString("vv-protoid-addwindow-title"));
        _addWindow.AddButtonPressed += OnAddButtonPressed;
        _addWindow.OpenCentered();
    }

    private void OnAddButtonPressed(ViewVariablesAddWindow.AddButtonPressedEventArgs args)
    {
        _lineEdit?.SetText(args.Entry);
        _addWindow?.Close();

        SetValue(args.Entry);
    }

    private void SetValue(string value)
    {
        var proto = (ProtoId<T>)value;
        if (_protoManager.HasIndex(proto))
            ValueChanged(proto, false);
    }
}
