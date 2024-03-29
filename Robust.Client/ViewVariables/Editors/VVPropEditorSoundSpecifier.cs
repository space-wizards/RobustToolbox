using System.Globalization;
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

    // Need to cache to some level just to make sure each edit doesn't reset the specifier to the default.

    private SoundSpecifier? _specifier;

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

        var pathControls = new BoxContainer()
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
                    _specifier = collection;
                    break;
                case SoundPathSpecifier path:
                    typeButton.SelectId(2);
                    editBox.Text = path.Path.ToString();
                    _specifier = path;
                    break;
                default:
                    _specifier = null;
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

                    _specifier = new SoundCollectionSpecifier(args.Text)
                    {
                        Params = _specifier?.Params ?? AudioParams.Default,
                    };
                    ValueChanged(_specifier);
                    break;
                case 2:
                    var path = new ResPath(args.Text);

                    if (!_resManager.ContentFileExists(path))
                        return;

                    _specifier = new SoundPathSpecifier(args.Text)
                    {
                        Params = _specifier?.Params ?? AudioParams.Default,
                    };

                    ValueChanged(_specifier);
                    break;
                default:
                    return;
            }
        };

        // Audio params

        /* Volume */

        var volumeEdit = new LineEdit()
        {
            Text = _specifier?.Params.Volume.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        volumeEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithVolume(floatValue);
            ValueChanged(_specifier);
        };

        var volumeContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-volume"),
                },
                volumeEdit,
            }
        };

        /* Pitch */

        var pitchEdit = new LineEdit()
        {
            Text = _specifier?.Params.Pitch.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        pitchEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithPitchScale(floatValue);
            ValueChanged(_specifier);
        };

        var pitchContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-pitch"),
                },
                pitchEdit,
            }
        };

        /* MaxDistance */

        var maxDistanceEdit = new LineEdit()
        {
            Text = _specifier?.Params.MaxDistance.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        maxDistanceEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithMaxDistance(floatValue);
            ValueChanged(_specifier);
        };

        var maxDistanceContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-max-distance"),
                },
                maxDistanceEdit,
            }
        };

        /* RolloffFactor */

        var rolloffFactorEdit = new LineEdit()
        {
            Text = _specifier?.Params.RolloffFactor.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        rolloffFactorEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithRolloffFactor(floatValue);
            ValueChanged(_specifier);
        };

        var rolloffFactorContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-rolloff-factor"),
                },
                rolloffFactorEdit,
            }
        };

        /* ReferenceDistance */

        var referenceDistanceEdit = new LineEdit()
        {
            Text = _specifier?.Params.ReferenceDistance.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        referenceDistanceEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithReferenceDistance(floatValue);
            ValueChanged(_specifier);
        };

        var referenceDistanceContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-reference-distance"),
                },
                referenceDistanceEdit,
            }
        };

        /* Loop */

        var loopButton = new Button()
        {
            Text = Loc.GetString("vv-sound-loop"),
            Pressed = _specifier?.Params.Loop ?? false,
            ToggleMode = true,
            Disabled = ReadOnly || _specifier == null,
        };

        loopButton.OnPressed += args =>
        {
            if (_specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithLoop(args.Button.Pressed);
            ValueChanged(_specifier);
        };

        /* PlayOffsetSeconds */

        var playOffsetEdit = new LineEdit()
        {
            Text = _specifier?.Params.PlayOffsetSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        playOffsetEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithPlayOffset(floatValue);
            ValueChanged(_specifier);
        };

        var playOffsetContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-play-offset"),
                },
                playOffsetEdit,
            }
        };

        /* Variation */

        var variationEdit = new LineEdit()
        {
            Text = _specifier?.Params.Variation.ToString() ?? string.Empty,
            HorizontalExpand = true,
            Editable = !ReadOnly && _specifier != null,
        };

        variationEdit.OnTextEntered += args =>
        {
            if (!float.TryParse(args.Text, out var floatValue) || _specifier == null)
                return;

            _specifier.Params = _specifier.Params.WithVariation(floatValue);
            ValueChanged(_specifier);
        };

        var variationContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = Loc.GetString("vv-sound-variation"),
                },
                variationEdit,
            }
        };

        var audioParamsControls = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                volumeContainer,
                pitchContainer,
                maxDistanceContainer,
                rolloffFactorContainer,
                referenceDistanceContainer,
                loopButton,
                playOffsetContainer,
                variationContainer,
            }
        };

        var controls = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                pathControls,
                audioParamsControls,
            }
        };

        return controls;
    }
}
