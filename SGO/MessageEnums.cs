using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public enum MessageResult
    {
        True,
        False,
        Ignored,
        Error
    }

    public enum MessageType
    {
        Empty,
        AddComponent,
        BoundKeyChange,
        BoundKeyRepeat,
        SlaveAttach,
        Click,
        SetSpriteByKey,
        IsCurrentHandEmpty,
        ItemToItemInteraction,
        ItemToLargeObjectInteraction,
        EmptyHandToItemInteraction,
        EmptyHandToLargeObjectInteraction
    }
}
