using System;
using System.Numerics;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
///     A LineEdit control that displays in a monospace font.
///     Because the font is monospace, this allows you to resize the control based on the size
///     of a given "test string", using <see cref="MeasureText"/>.
/// </summary>
[Virtual]
public partial class MonoLineEdit : LineEdit
{
    public string? MeasureText { get; set; } = null;

    public MonoLineEdit()
    {
        AddStyleClass("monospace");
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var baseOverride = base.MeasureOverride(availableSize);

        if (MeasureText is null)
            return baseOverride;

        var font = GetFont();
        var textOverride = new Vector2(1.0f, 0.0f);
        foreach (var rune in MeasureText.EnumerateRunes())
        {
            if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                continue;

            textOverride += new Vector2(metrics.Advance, 0);
        }

        return baseOverride + textOverride;
    }
}
