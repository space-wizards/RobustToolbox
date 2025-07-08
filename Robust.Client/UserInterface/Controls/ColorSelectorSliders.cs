using System;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls;

// condensed version of the original ColorSlider set
public sealed class ColorSelectorSliders : Control
{
    // TODO: This might be desyncing from _currentColor when sliders change.
    public Color Color
    {
        get => _currentColor;
        set
        {
            _currentColor = value;
            _colorData = _strategy.ToColorData(value);

            Update();
        }
    }

    public ColorSelectorType SelectorType
    {
        get => _currentType;
        set
        {
            _currentType = value;
            _typeSelector.Select(_types.IndexOf(value));

            _strategy = GetStrategy(value);
            _colorData = _strategy.ToColorData(_currentColor);

            UpdateType();
            Update();
        }
    }

    private IColorSliderStrategy _strategy { get; set; }

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

    private const float AlphaDivisor = 100.0f;

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

        _topColorSlider.OnValueChanged += r => { OnSliderValueChanged(ColorSliderOrder.Top); };
        _middleColorSlider.OnValueChanged += r => { OnSliderValueChanged(ColorSliderOrder.Middle); };
        _bottomColorSlider.OnValueChanged += r => { OnSliderValueChanged(ColorSliderOrder.Bottom); };
        _alphaSlider.OnValueChanged += r => { OnSliderValueChanged(ColorSliderOrder.Alpha); };

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

        _topInputBox.ValueChanged += value => OnInputBoxValueChanged(value, ColorSliderOrder.Top);
        _middleInputBox.ValueChanged += value => OnInputBoxValueChanged(value, ColorSliderOrder.Middle);
        _bottomInputBox.ValueChanged += value => OnInputBoxValueChanged(value, ColorSliderOrder.Bottom);
        _alphaInputBox.ValueChanged += value => OnInputBoxValueChanged(value, ColorSliderOrder.Alpha);

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

        _strategy = GetStrategy(SelectorType);
        Color = _currentColor;
    }

    private IColorSliderStrategy GetStrategy(ColorSelectorType selectorType)
    {
        return selectorType switch
        {
            ColorSelectorType.Rgb => new RgbSliderStategy(),
            ColorSelectorType.Hsv => new HsvSliderStategy(),
            _ => throw new NotImplementedException(),
        };
    }

    private (Slider slider, SpinBox inputBox) GetSliderByOrder(ColorSliderOrder order)
    {
        return order switch
        {
            ColorSliderOrder.Top => (_topColorSlider, _topInputBox),
            ColorSliderOrder.Middle => (_middleColorSlider, _middleInputBox),
            ColorSliderOrder.Bottom => (_bottomColorSlider, _bottomInputBox),
            ColorSliderOrder.Alpha => (_alphaSlider, _alphaInputBox),
            _ => throw new NotImplementedException(),
        };
    }

    private void UpdateType()
    {
        var labels = _strategy.GetSliderLabelTexts();
        _topSliderLabel.Text = labels.top;
        _middleSliderLabel.Text = labels.middle;
        _bottomSliderLabel.Text = labels.bottom;

        _topStyle.ConfigureSlider(_strategy.TopSliderStyle);
        _middleStyle.ConfigureSlider(_strategy.MiddleSliderStyle);
        _bottomStyle.ConfigureSlider(_strategy.BottomSliderStyle);
    }

    private void UpdateSlider(ColorSliderOrder order)
    {
        var (slider, inputBox) = GetSliderByOrder(order);
        var sliderValues = _strategy.GetSliderValues(_colorData);
        var inputBoxes = _strategy.GetInputBoxValues(_colorData);

        var value = 0.0f;
        var inputBoxValue = 0;

        switch (order)
        {
            case ColorSliderOrder.Top:
                value = sliderValues.top;
                inputBoxValue = (int)inputBoxes.top;
                break;
            case ColorSliderOrder.Middle:
                value = sliderValues.middle;
                inputBoxValue = (int)inputBoxes.middle;
                break;
            case ColorSliderOrder.Bottom:
                value = sliderValues.bottom;
                inputBoxValue = (int)inputBoxes.bottom;
                break;
            case ColorSliderOrder.Alpha:
                value = _currentColor.A;
                inputBoxValue = (int)(_currentColor.A * AlphaDivisor);
                break;
        }

        slider.Value = value;
        inputBox.Value = inputBoxValue;
    }

    private void Update()
    {
        // This code is a mess of UI events causing stack overflows. Also, updating one slider triggers all sliders to
        // update, which due to rounding errors causes them to actually change values, specifically for HSV sliders.
        if (_updating)
            return;

        _updating = true;

        UpdateSlider(ColorSliderOrder.Top);
        UpdateSlider(ColorSliderOrder.Middle);
        UpdateSlider(ColorSliderOrder.Bottom);
        UpdateSlider(ColorSliderOrder.Alpha);

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
            return value <= AlphaDivisor;
        }

        return _strategy.IsSliderInputValid(value, ordering);
    }

    private float GetColorValueDivisor(ColorSliderOrder order)
    {
        if (order == ColorSliderOrder.Alpha)
        {
            return AlphaDivisor;
        }

        return _strategy.GetColorValueDivisor(order);
    }

    private void OnInputBoxValueChanged(ValueChangedEventArgs args, ColorSliderOrder order)
    {
        var (slider, _) = GetSliderByOrder(order);
        var value = args.Value / GetColorValueDivisor(order);

        slider.Value = value;
    }

    private void OnSliderValueChanged(ColorSliderOrder order)
    {
        if (_updating)
            return;
        _updating = true;

        _colorData = new Vector4(
            _topColorSlider.Value,
            _middleColorSlider.Value,
            _bottomColorSlider.Value,
            _alphaSlider.Value);

        _currentColor = _strategy.FromColorData(_colorData);
        OnColorChanged?.Invoke(_currentColor);

        UpdateSlider(order);
        _updating = false;
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

    private interface IColorSliderStrategy
    {
        public ColorSelectorStyleBox.ColorSliderPreset TopSliderStyle { get; }
        public ColorSelectorStyleBox.ColorSliderPreset MiddleSliderStyle { get; }
        public ColorSelectorStyleBox.ColorSliderPreset BottomSliderStyle { get; }

        public Vector4 ToColorData(Color color);
        public Color FromColorData(Vector4 colorData);

        public bool IsSliderInputValid(int value, ColorSliderOrder order);
        public float GetColorValueDivisor(ColorSliderOrder order);

        public (string top, string middle, string bottom) GetSliderLabelTexts();
        public (float top, float middle, float bottom) GetSliderValues(Vector4 colorData);
        public (float top, float middle, float bottom) GetInputBoxValues(Vector4 colorData);
    }

    private sealed class RgbSliderStategy : IColorSliderStrategy
    {
        private const float ChannelMaxValue = byte.MaxValue;

        public ColorSelectorStyleBox.ColorSliderPreset TopSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Red;
        public ColorSelectorStyleBox.ColorSliderPreset MiddleSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Green;
        public ColorSelectorStyleBox.ColorSliderPreset BottomSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Blue;

        public Vector4 ToColorData(Color color) => new(color.R, color.G, color.B, color.A);
        public Color FromColorData(Vector4 colorData) => new(colorData.X, colorData.Y, colorData.Z, colorData.W);

        public bool IsSliderInputValid(int value, ColorSliderOrder order) => value <= ChannelMaxValue;
        public float GetColorValueDivisor(ColorSliderOrder order) => ChannelMaxValue;

        public (string top, string middle, string bottom) GetSliderLabelTexts()
        {
            return (
                Loc.GetString("color-selector-sliders-red"),
                Loc.GetString("color-selector-sliders-green"),
                Loc.GetString("color-selector-sliders-blue"));
        }

        public (float top, float middle, float bottom) GetSliderValues(Vector4 colorData)
        {
            return (colorData.X, colorData.Y, colorData.Z);
        }

        public (float top, float middle, float bottom) GetInputBoxValues(Vector4 colorData)
        {
            var topDivisor = GetColorValueDivisor(ColorSliderOrder.Top);
            var middleDivisor = GetColorValueDivisor(ColorSliderOrder.Middle);
            var bottomDivisor = GetColorValueDivisor(ColorSliderOrder.Bottom);
            var sliderValues = GetSliderValues(colorData);

            return (
                sliderValues.top * topDivisor,
                sliderValues.middle * middleDivisor,
                sliderValues.bottom * bottomDivisor);
        }
    }

    private sealed class HsvSliderStategy : IColorSliderStrategy
    {
        private const float HueMaxValue = 360.0f;
        private const float SliderMaxValue = 100.0f;

        public ColorSelectorStyleBox.ColorSliderPreset TopSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Hue;
        public ColorSelectorStyleBox.ColorSliderPreset MiddleSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Saturation;
        public ColorSelectorStyleBox.ColorSliderPreset BottomSliderStyle
            => ColorSelectorStyleBox.ColorSliderPreset.Value;

        public Vector4 ToColorData(Color color) => Color.ToHsv(color);
        public Color FromColorData(Vector4 colorData) => Color.FromHsv(colorData);

        public bool IsSliderInputValid(int value, ColorSliderOrder order)
        {
            return order switch
            {
                ColorSliderOrder.Top => value <= HueMaxValue,
                _ => value <= SliderMaxValue,
            };
        }

        public float GetColorValueDivisor(ColorSliderOrder order)
        {
            return order switch
            {
                ColorSliderOrder.Top => HueMaxValue,
                _ => SliderMaxValue,
            };
        }

        public (string top, string middle, string bottom) GetSliderLabelTexts()
        {
            return (
                Loc.GetString("color-selector-sliders-hue"),
                Loc.GetString("color-selector-sliders-saturation"),
                Loc.GetString("color-selector-sliders-value"));
        }

        public (float top, float middle, float bottom) GetSliderValues(Vector4 colorData)
        {
            return (colorData.X, colorData.Y, colorData.Z);
        }

        public (float top, float middle, float bottom) GetInputBoxValues(Vector4 colorData)
        {
            var topDivisor = GetColorValueDivisor(ColorSliderOrder.Top);
            var middleDivisor = GetColorValueDivisor(ColorSliderOrder.Middle);
            var bottomDivisor = GetColorValueDivisor(ColorSliderOrder.Bottom);
            var sliderValues = GetSliderValues(colorData);

            return (
                sliderValues.top * topDivisor,
                sliderValues.middle * middleDivisor,
                sliderValues.bottom * bottomDivisor);
        }
    }
}
