using System;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls;

// condensed version of the original ColorSlider set
public sealed class ColorSelectorSliders : Control
{
    public Color Color
    {
        get => _currentColor;
        set
        {
            _currentColor = value;
            switch (SelectorType)
            {
                case ColorSelectorType.Rgb:
                    _colorData = new Vector4(_currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A);
                    break;
                case ColorSelectorType.Hsv:
                    _colorData = Color.ToHsv(value);
                    break;
            }
            Update();
        }
    }

    public ColorSelectorType SelectorType
    {
        get => _currentType;
        set
        {
            switch ((_currentType, value))
            {
                case (ColorSelectorType.Rgb, ColorSelectorType.Hsv):
                    _colorData = Color.ToHsv(Color);
                    break;
                case (ColorSelectorType.Hsv, ColorSelectorType.Rgb):
                    _colorData = new Vector4(_currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A);
                    break;
            }
            _currentType = value;
            UpdateType();
            Update();
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
    private Vector4 _colorData;
    private ColorSelectorType _currentType = ColorSelectorType.Rgb;
    private bool _isAlphaVisible = false;

    private ColorableSlider _topColorSlider;
    private ColorableSlider _middleColorSlider;
    private ColorableSlider _bottomColorSlider;
    private Slider _alphaSlider;

    private BoxContainer _alphaSliderBox = new();

    private SpinBox _topInputBox;
    private SpinBox _middleInputBox;
    private SpinBox _bottomInputBox;
    private SpinBox _alphaInputBox;

    private Label _topSliderLabel = new();
    private Label _middleSliderLabel = new();
    private Label _bottomSliderLabel = new();
    private Label _alphaSliderLabel = new();

    private OptionButton _typeSelector;
    private List<ColorSelectorType> _types = new();

    private ColorSelectorStyleBox _topStyle;
    private ColorSelectorStyleBox _middleStyle;
    private ColorSelectorStyleBox _bottomStyle;

    public ColorSelectorSliders()
    {
        _topColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            BackgroundStyleBoxOverride = _topStyle = new(),
            MaxValue = 1.0f
        };

        _middleColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            BackgroundStyleBoxOverride = _middleStyle = new(),
            MaxValue = 1.0f
        };

        _bottomColorSlider = new ColorableSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            BackgroundStyleBoxOverride = _bottomStyle = new(),
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

        _topInputBox = new SpinBox
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Top)
        };
        _topInputBox.InitDefaultButtons();

        _middleInputBox = new SpinBox
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Middle)
        };
        _middleInputBox.InitDefaultButtons();

        _bottomInputBox = new SpinBox
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Bottom)
        };
        _bottomInputBox.InitDefaultButtons();

        _alphaInputBox = new SpinBox
        {
            IsValid = value => IsSpinBoxValid(value, ColorSliderOrder.Alpha)
        };
        _alphaInputBox.InitDefaultButtons();

        _topInputBox.ValueChanged += value =>
        {
            _topColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Top);
        };

        _middleInputBox.ValueChanged += value =>
        {
            _middleColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Middle);
        };

        _bottomInputBox.ValueChanged += value =>
        {
            _bottomColorSlider.Value = value.Value / GetColorValueDivisor(ColorSliderOrder.Bottom);
        };

        _alphaInputBox.ValueChanged += value =>
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

        UpdateType();
        Color = _currentColor;
    }

    private void UpdateType()
    {
        (string topLabel, string middleLabel, string bottomLabel) labels = GetSliderLabels();

        _topSliderLabel.Text = labels.topLabel;
        _middleSliderLabel.Text = labels.middleLabel;
        _bottomSliderLabel.Text = labels.bottomLabel;

        bool hsv = SelectorType == ColorSelectorType.Hsv;
        _topStyle.ConfigureSlider( hsv ? ColorSelectorStyleBox.ColorSliderPreset.Hue : ColorSelectorStyleBox.ColorSliderPreset.Red);
        _middleStyle.ConfigureSlider( hsv ? ColorSelectorStyleBox.ColorSliderPreset.Saturation : ColorSelectorStyleBox.ColorSliderPreset.Green);
        _bottomStyle.ConfigureSlider( hsv ? ColorSelectorStyleBox.ColorSliderPreset.Value : ColorSelectorStyleBox.ColorSliderPreset.Blue);
    }

    private void Update()
    {
        // This code is a mess of UI events causing stack overflows. Also, updating one slider triggers all sliders to
        // update, which due to rounding errors causes them to actually change values, specifically for HSV sliders.
        if (_updating)
            return;

        _updating = true;
        _topStyle.SetBaseColor(_colorData);
        _middleStyle.SetBaseColor(_colorData);
        _bottomStyle.SetBaseColor(_colorData);

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                _topColorSlider.Value = _colorData.X;
                _middleColorSlider.Value = _colorData.Y;
                _bottomColorSlider.Value = _colorData.Z;

                _topInputBox.Value = (int)(_colorData.X * 255.0f);
                _middleInputBox.Value = (int)(_colorData.Y * 255.0f);
                _bottomInputBox.Value = (int)(_colorData.Z * 255.0f);

                break;
            case ColorSelectorType.Hsv:
                // dumb workaround because the formula for
                // HSV calculation results in a negative
                // number in any value past 300 degrees
                if (_colorData.X > 0)
                {
                    _topColorSlider.Value = _colorData.X;
                    _topInputBox.Value = (int)(_colorData.X * 360.0f);
                }
                else
                {
                    _topInputBox.Value = (int)(_topColorSlider.Value * 360.0f);
                }

                _middleColorSlider.Value = _colorData.Y;
                _bottomColorSlider.Value = _colorData.Z;

                _middleInputBox.Value = (int)(_colorData.Y * 100.0f);
                _bottomInputBox.Value = (int)(_colorData.Z * 100.0f);


                break;
        }

        _alphaSlider.Value = Color.A;
        _alphaInputBox.Value = (int)(Color.A * 100.0f);
        _updating = false;
    }

    private bool IsSpinBoxValid(int value, ColorSliderOrder ordering)
    {
        if (value < 0)
        {
            return false;
        }

        if (ordering == ColorSliderOrder.Alpha)
        {
            return value <= 100;
        }

        switch (SelectorType)
        {
            case ColorSelectorType.Rgb:
                return value <= byte.MaxValue;
            case ColorSelectorType.Hsv:
                switch (ordering)
                {
                    case ColorSliderOrder.Top:
                        return value <= 360;
                    default:
                        return value <= 100;
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

        _colorData = new Vector4(_topColorSlider.Value, _middleColorSlider.Value, _bottomColorSlider.Value, _alphaSlider.Value);

        _currentColor = SelectorType switch
        {
            ColorSelectorType.Hsv => Color.FromHsv(_colorData),
            _ => new Color(_colorData.X, _colorData.Y, _colorData.Z, _colorData.W)
        };

        Update();
        OnColorChanged?.Invoke(_currentColor);
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
