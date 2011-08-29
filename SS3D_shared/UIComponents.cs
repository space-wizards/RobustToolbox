using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared
{
    public enum GuiComponentType
    {
        HealthComponent,
        AppendagesComponent,
        StatPanelComponent
    }

    public enum HealthComponentMessage
    {
        CurrentHealth
    }

    public enum HandsComponentMessage
    {
        SelectHand,
        UpdateHandObjects
    }
}
