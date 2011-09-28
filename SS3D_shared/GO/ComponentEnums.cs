using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.GO
{
    /// <summary>
    /// Component Family ie. what type of component it is.
    /// </summary>
    public enum ComponentFamily
    {
        Generic,
        Input,
        Mover,
        Click,
        Inventory,
        Equipment,
        Item,
        Hands,
        Tool,
        Wearable,
        Health,
        Renderable,
    }
}
