using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Editors;

public sealed class VVPropEditorSoundSpecifier : VVPropEditor
{
    private readonly IPrototypeManager _protoManager;
    private readonly IResourceManager _resManager;

    public VVPropEditorSoundSpecifier(IPrototypeManager protoManager, IResourceManager resManager)
    {
        _protoManager = protoManager;
        _resManager = resManager;
    }

    protected override Control MakeUI(object? value)
    {
        var typeButton = new OptionButton()
        {
            Disabled = ReadOnly,
        };

        typeButton.AddItem(Loc.GetString("vv-sound-none"));
        typeButton.AddItem(Loc.GetString("vv-sound-collection"), 1);
        typeButton.AddItem(Loc.GetString("vv-sound-path"), 2);

        var editBox = new LineEdit()
        {
            HorizontalExpand = true,
            Editable = !ReadOnly,
        };

        var controls = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                typeButton,
                editBox
            },
            SetSize = new Vector2(384f, 32f)
        };

        if (value != null)
        {
            switch (value)
            {
                case SoundCollectionSpecifier collection:
                    typeButton.SelectId(1);
                    editBox.Text = collection.Collection ?? string.Empty;
                    break;
                case SoundPathSpecifier path:
                    typeButton.SelectId(2);
                    editBox.Text = path.Path.ToString();
                    break;
            }
        }

        typeButton.OnItemSelected += args =>
        {
            typeButton.SelectId(args.Id);
            editBox.Text = string.Empty;

            editBox.Editable = !ReadOnly && typeButton.SelectedId > 0;

            if (typeButton.SelectedId == 0)
            {
                // Dummy value
                ValueChanged(new SoundPathSpecifier(""));
            }
        };

        editBox.OnTextEntered += args =>
        {
            if (string.IsNullOrEmpty(args.Text))
                return;

            switch (typeButton.SelectedId)
            {
                case 1:
                    if (!_protoManager.HasIndex<SoundCollectionPrototype>(args.Text))
                        return;

                    ValueChanged(new SoundCollectionSpecifier(args.Text));
                    break;
                case 2:
                    var path = new ResPath(args.Text);

                    if (!_resManager.ContentFileExists(path))
                        return;

                    ValueChanged(new SoundPathSpecifier(args.Text));
                    break;
                default:
                    return;
            }
        };

        return controls;
    }
}
