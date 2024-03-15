using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

public sealed class DevWindowPopup : Popup
{
    public readonly Label TextLabel = new();
    public readonly PanelContainer Panel = new();

    public string? Text
    {
        get => TextLabel.Text;
        set => TextLabel.Text = value;
    }

    public DevWindowPopup()
    {
        Panel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#40404C")
        };
        AddChild(Panel);
        AddChild(TextLabel);
    }
}
