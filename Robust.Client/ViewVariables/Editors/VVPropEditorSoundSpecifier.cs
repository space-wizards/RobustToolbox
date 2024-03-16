using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Editors;

public sealed class VVPropEditorSoundSpecifier : VVPropEditor
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    protected override Control MakeUI(object? value)
    {
        var typeButton = new OptionButton()
        {
            Disabled = ReadOnly,
        };
        typeButton.AddItem(Loc.GetString("vv-sound-none"));
        typeButton.AddItem(Loc.GetString("vv-sound-collection"));
        typeButton.AddItem(Loc.GetString("vv-sound-path"));

        var editBox = new LineEdit()
        {
            HorizontalExpand = true,
            Editable = !ReadOnly,
        };

        var controls = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                typeButton,
                editBox
            },
            SetSize = new Vector2(256f, 64f)
        };

        if (value != null)
        {
            switch (value)
            {
                case ViewVariablesBlobMembers.SoundSpecifierReferenceToken token:
                    switch (token.Variant)
                    {
                        case "SoundCollectionSpecifier":
                            typeButton.Select(1);
                            editBox.Text = token.Value;
                            break;
                        case "SoundPathSpecifier":
                            typeButton.Select(2);
                            editBox.Text = token.Value;
                            break;
                    }

                    break;
                case SoundCollectionSpecifier collection:
                    typeButton.Select(1);
                    editBox.Text = collection.Collection ?? string.Empty;
                    break;
                case SoundPathSpecifier path:
                    typeButton.Select(2);
                    editBox.Text = path.Path.ToString();
                    break;
            }
        }

        typeButton.OnItemSelected += args =>
        {
            typeButton.SelectId(args.Id);
            editBox.Text = string.Empty;
        };

        editBox.OnTextEntered += args =>
        {
            if (string.IsNullOrEmpty(args.Text))
                return;

            switch (typeButton.SelectedId)
            {
                case 1:
                    ValueChanged(new SoundCollectionSpecifier(args.Text));
                    break;
                case 2:
                    ValueChanged(new SoundPathSpecifier(args.Text));
                    break;
                default:
                    return;
            }
        };

        return controls;
    }
}
