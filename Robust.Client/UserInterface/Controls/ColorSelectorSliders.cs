using System;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.Controls;

// condensed version of the original ColorSlider set
public sealed class ColorSelectorSliders : Control
{
    public Color Color
    {
        get => _currentColor;
        set
        {
            _updating = true;
            _currentColor = value;

            Update();
            _updating = false;
        }
    }

    public ColorSelectorType SelectorType
    {
        get => _currentType;
        set
        {
            _updating = true;
            _currentType = value;

            UpdateType();
            Update();
            _updating = false;
        }
    }

    public bool IsAlphaVisible
    {
        get => _isAlphaVisible;
        set
        {
            _isAlphaVisible = value;

            _alphaSliderBox.Visible = _isAlphaVisible;
        }
    }

    public Action<Color>? OnColorChanged;

    private bool _updating = false;
    private Color _currentColor = Color.White;
    private ColorSelectorType _currentType = ColorSelectorType.Rgb;
    private bool _isAlphaVisible = false;

    private ColorableSlider _topColorSlider;
    private ColorableSlider _middleColorSlider;
    private ColorableSlider _bottomColorSlider;
    private Slider _alphaSlider;

    private BoxContainer _alphaSliderBox = new();

    private FloatSpinBox _topInputBox;
    private FloatSpinBox _middleInputBox;
    private FloatSpinBox _bottomInputBox;
    private FloatSpinBox _alphaInputBox;

    private Label _topSliderLabel = new();
    private Label _middleSliderLabel = new();
    private Label _bottomSliderLabel = new();
    private Label _alphaSliderLabel = new();

    private OptionButton _typeSelector;
    private List<ColorSelectorType> _types = new();

    public ColorSelectorSliders()
    {
        _topColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MaxValue = 1.0f
        };

        _middleColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MaxValue = 1.0f
        };

        _bottomColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MaxValue = 1.0f
        };

        _alphaSlider = new Slider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MaxValue = 1.0f,
        };

        _topColorSlider.OnValueChanged += _ => { OnColorSet(); };
        _middleColorSlider.OnValueChanged += _ => { OnColorSet(); };
        _bottomColorSlider.OnValueChanged += _ => { OnColorSet(); };
        _alphaSlider.OnValueChanged += _ => { OnColorSet(); };

        _topInputBox = new FloatSpinBox(1f, 2)
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Top)
        };

        _middleInputBox = new FloatSpinBox(1f, 2)
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Middle)
        };

        _bottomInputBox = new FloatSpinBox(1f, 2)
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Bottom)
        };

        _alphaInputBox = new FloatSpinBox(1f, 2)
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Alpha)
        };

        _topInputBox.OnValueChanged += value =>
        {
            _topColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Top);
        };

        _middleInputBox.OnValueChanged += value =>
        {
            _middleColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Middle);
        };

        _bottomInputBox.OnValueChanged += value =>
        {
            _bottomColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Bottom);
        };

        _alphaInputBox.OnValueChanged += value =>
        {
            _alphaSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Alpha);
        };

        _alphaSliderLabel.Text = Loc.GetString("color-selector-sliders-alpha");

        _typeSelector = new OptionButton();
        foreach (var ty in Enum.GetValues<ColorSelectorType>())
        {
            _typeSelector.AddItem(Loc.GetString($"color-selector-sliders-{ty.ToString().ToLower()}"));
            _types.Add(ty);
        }

        _typeSelector.OnItemSelected += args =>
        {
            SelectorType = _types[args.Id];
            _typeSelector.Select(args.Id);
        };

        // TODO: Maybe some engine widgets could be laid out in XAML?

        var rootBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(rootBox);

        var headerBox = new BoxContainer();
        rootBox.AddChild(headerBox);

        headerBox.AddChild(_typeSelector);

        var bodyBox = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        // pita
        var topSliderBox = new BoxContainer();

        topSliderBox.AddChild(_topSliderLabel);
        topSliderBox.AddChild(_topColorSlider);
        topSliderBox.AddChild(_topInputBox);

        var middleSliderBox = new BoxContainer();

        middleSliderBox.AddChild(_middleSliderLabel);
        middleSliderBox.AddChild(_middleColorSlider);
        middleSliderBox.AddChild(_middleInputBox);

        var bottomSliderBox = new BoxContainer();

        bottomSliderBox.AddChild(_bottomSliderLabel);
        bottomSliderBox.AddChild(_bottomColorSlider);
        bottomSliderBox.AddChild(_bottomInputBox);

        _alphaSliderBox.Visible = IsAlphaVisible;
        _alphaSliderBox.AddChild(_alphaSliderLabel);
        _alphaSliderBox.AddChild(_alphaSlider);
        _alphaSliderBox.AddChild(_alphaInputBox);

        bodyBox.AddChild(topSliderBox);
        bodyBox.AddChild(middleSliderBox);
        bodyBox.AddChild(bottomSliderBox);
        bodyBox.AddChild(_alphaSliderBox);

        rootBox.AddChild(bodyBox);

        _updating = true;
        UpdateType();
        Update();
        _updating = false;
    }

    private void UpdateType()
    {
        (string topLabel, string middleLabel, string bottomLabel) labels = GetSliderLabels();

        _topSliderLabel.Text = labels.topLabel;
        _middleSliderLabel.Text = labels.middleLabel;
        _bottomSliderLabel.Text = labels.bottomLabel;
    }

    private void Update()
    {
        _topColorSlider.SetColor(_currentColor);
        _middleColorSlider.SetColor(_currentColor);
        _bottomColorSlider.SetColor(_currentColor);

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                _topColorSlider.Value = Color.R;
                _middleColorSlider.Value = Color.G;
                _bottomColorSlider.Value = Color.B;

                _topInputBox.Value = Color.R * 255.0f;
                _middleInputBox.Value = Color.G * 255.0f;
                _bottomInputBox.Value = Color.B * 255.0f;

                break;
            case ColorSelectorType.Hsv:
                Vector4 color = Color.ToHsv(Color);

                // dumb workaround because the formula for
                // HSV calculation results in a negative
                // number in any value past 300 degrees
                if (color.X > 0)
                {
                    _topColorSlider.Value = color.X;
                    _topInputBox.Value = color.X * 360.0f;
                }
                else
                {
                    _topInputBox.Value = _topColorSlider.Value * 360.0f;
                }

                _middleColorSlider.Value = color.Y;
                _bottomColorSlider.Value = color.Z;

                _middleInputBox.Value = color.Y * 100.0f;
                _bottomInputBox.Value = color.Z * 100.0f;


                break;
        }

        _alphaSlider.Value = Color.A;
        _alphaInputBox.Value = Color.A * 100.0f;
    }

    private bool IsSpinBoxValid(float value, ColorSliderOrder ordering)
    {
        if (value < 0)
        {
            return false;
        }

        if (ordering == ColorSliderOrder.Alpha)
        {
            return value <= 100.0f;
        }

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                return value <= byte.MaxValue;
            case ColorSelectorType.Hsv:
                switch (ordering)
                {
                    case ColorSliderOrder.Top:
                        return value <= 360.0f;
                    default:
                        return value <= 100.0f;
                }
        }

        return false;
    }

    private (string, string, string) GetSliderLabels()
    {
        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                return (
                    Loc.GetString("color-selector-sliders-red"),
                    Loc.GetString("color-selector-sliders-green"),
                    Loc.GetString("color-selector-sliders-blue")
                );
            case ColorSelectorType.Hsv:
                return (
                    Loc.GetString("color-selector-sliders-hue"),
                    Loc.GetString("color-selector-sliders-saturation"),
                    Loc.GetString("color-selector-sliders-value")
                );
        }

        return ("ERR", "ERR", "ERR");
    }

    private float GetColorValueDivisor(ColorSliderOrder order)
    {
        if (order == ColorSliderOrder.Alpha)
        {
            return 100.0f;
        }

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                return 255.0f;
            case ColorSelectorType.Hsv:
                switch (order)
                {
                    case ColorSliderOrder.Top:
                        return 360.0f;
                    default:
                        return 100.0f;
                }
        }

        return 0.0f;
    }

    private void OnColorSet()
    {
        // stack overflow otherwise due to value sets
        if (_updating)
        {
            return;
        }

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                Color rgbColor = new Color(_topColorSlider.Value, _middleColorSlider.Value, _bottomColorSlider.Value, _alphaSlider.Value);

                _currentColor = rgbColor;
                Update();

                OnColorChanged!(rgbColor);
                break;
            case ColorSelectorType.Hsv:
                Color hsvColor = Color.FromHsv(new Vector4(_topColorSlider.Value, _middleColorSlider.Value, _bottomColorSlider.Value, _alphaSlider.Value));

                _currentColor = hsvColor;
                Update();

                OnColorChanged!(hsvColor);
                break;
        }
    }

    private enum ColorSliderOrder
    {
        Top,
        Middle,
        Bottom,
        Alpha
    }

    public enum ColorSelectorType
    {
        Rgb,
        Hsv,
    }
}
