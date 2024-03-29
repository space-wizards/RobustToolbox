﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Robust.Client.UserInterface;

[GenerateTypedNameReferences]
internal sealed partial class DevWindowTabUIPopup : Popup
{
    public string? Text
    {
        get => TextLabel.Text;
        set => TextLabel.Text = value;
    }

    public DevWindowTabUIPopup()
    {
        RobustXamlLoader.Load(this);
    }
}
