using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.Controls;

public sealed class ColorableSlider : Slider
{
    public const string StylePropertyFillWhite = "fillWhite"; // needs to be filled with white
    public const string StylePropertyBackgroundWhite = "backgroundWhite"; // also needs to be filled with white

    public Color Color { get; private set; } = Color.White;
    public PartSelector Part
    {
        get => _currentPartSelector;
        set
        {
            _currentPartSelector = value;

            UpdateStyleBoxes();
        }
    }

    private PartSelector _currentPartSelector = PartSelector.Background;

    public void SetColor(Color color)
    {
        Color = color;
        switch (Part)
        {
            case PartSelector.Fill:
                _fillPanel.Modulate = Color;
                break;
            case PartSelector.Background:
                _backgroundPanel.Modulate = Color;
                break;
        }
    }

    protected override void UpdateStyleBoxes()
    {
        StyleBox? GetStyleBox(string name)
        {
            if (TryGetStyleProperty<StyleBox>(name, out var box))
            {
                return box;
            }

            return null;
        }

        string backBox = StylePropertyBackground;
        string fillBox = StylePropertyFill;

        switch (Part)
        {
            case PartSelector.Fill:
                fillBox = StylePropertyFillWhite;
                _fillPanel.Modulate = Color;

                break;
            case PartSelector.Background:
                backBox = StylePropertyBackgroundWhite;

                _fillPanel.Modulate = Color.Transparent; // make this transparent
                _backgroundPanel.Modulate = Color;
                break;
        }

        _backgroundPanel.PanelOverride = BackgroundStyleBoxOverride ?? GetStyleBox(backBox);
        _foregroundPanel.PanelOverride = ForegroundStyleBoxOverride ?? GetStyleBox(StylePropertyForeground);
        _fillPanel.PanelOverride = FillStyleBoxOverride ?? GetStyleBox(fillBox);
        _grabber.PanelOverride = GrabberStyleBoxOverride ?? GetStyleBox(StylePropertyGrabber);
    }

    public enum PartSelector
    {
        Fill,
        Background
    }
}
