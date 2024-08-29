using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// Style box for colouring sliders and 2-d colour selectors. E.g., this could be used to draw the typical HSV colour
/// selection rainbow.
/// </summary>
public sealed class ColorSelectorStyleBox : StyleBoxTexture
{
    public const string TexturePath = "/Textures/Interface/Nano/slider_fill.svg.96dpi.png";
    public static ProtoId<ShaderPrototype> Prototype = "ColorPicker";

    private ShaderInstance _shader;

    /// <summary>
    /// Base background colour.
    /// </summary>
    public Robust.Shared.Maths.Vector4 BaseColor;

    /// <summary>
    /// Colour to add to the background colour along the X-axis.
    /// I.e., from left to right the background colour will vary from (BaseColour) to (BaseColour + XAxis)
    /// </summary>
    public Robust.Shared.Maths.Vector4 XAxis;

    /// <summary>
    /// Colour to add to the background colour along the y-axis.
    /// I.e., from left to right the background colour will vary from (BaseColour) to (BaseColour + XAxis)
    /// </summary>
    public Robust.Shared.Maths.Vector4 YAxis;

    /// <summary>
    /// If true, then <see cref="BaseColor"/>, <see cref="XAxis"/>, and <see cref="YAxis"/> will be interpreted as HSVa
    /// colours.
    /// </summary>
    public bool Hsv;

    public ColorSelectorStyleBox(ColorSliderPreset preset = ColorSliderPreset.Red)
    {
        Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(TexturePath);
        _shader = IoCManager.Resolve<IPrototypeManager>().Index(Prototype).InstanceUnique();
        SetPatchMargin(Margin.All, 12);
        ConfigureSlider(preset);
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var old = handle.GetShader();
        handle.UseShader(_shader);

        var globalPixelPos = Vector2.Transform(default, handle.GetTransform());
        _shader.SetParameter("size", box.Size);
        _shader.SetParameter("offset", globalPixelPos);
        _shader.SetParameter("xAxis", XAxis);
        _shader.SetParameter("yAxis", YAxis);
        _shader.SetParameter("baseColor", BaseColor);
        _shader.SetParameter("hsv", Hsv);

        base.DoDraw(handle, box, uiScale);
        handle.UseShader(old);
    }

    public void ConfigureSlider(ColorSliderPreset preset)
    {
        Hsv = preset > ColorSliderPreset.Blue;

        if (preset == ColorSliderPreset.HueValue)
        {
            XAxis = new(1, 0, 0, 0); // Hue;
            YAxis = new(0, 0, 1, 0); // value;
            return;
        }

        YAxis = default;
        XAxis = preset switch
        {
            ColorSliderPreset.Red or ColorSliderPreset.Hue => new(1, 0, 0, 0),
            ColorSliderPreset.Green or ColorSliderPreset.Saturation => new(0, 1, 0, 0),
            _ => new(0, 0, 1, 0),
        };
    }

    /// <summary>
    /// Helper method that sets the base color by taking in some color and removing the components that are controlled by the x and y axes.
    /// </summary>
    public void SetBaseColor(Color color)
    {
        var colorData = Hsv
            ? Color.ToHsv(color)
            : new Robust.Shared.Maths.Vector4(color.R, color.G, color.B, color.A);
        SetBaseColor(colorData);
    }

    /// <summary>
    /// Helper method that sets the base color by taking in some color and removing the components that are controlled by the x and y axes.
    /// </summary>
    public void SetBaseColor(Robust.Shared.Maths.Vector4 colorData)
    {
        BaseColor = colorData - colorData * XAxis - colorData * YAxis;
    }

    public enum ColorSliderPreset : byte
    {
        // Horizontal red slider
        Red = 1,

        // Horizontal green slider
        Green = 2,

        // Horizontal blue slider
        Blue = 3,

        // Horizontal hue slider
        Hue = 4,

        // Horizontal situation slider
        Saturation = 5,

        // Horizontal saturation slider
        Value = 6,

        // 2-D hue-value box
        HueValue = 7
    }
}
