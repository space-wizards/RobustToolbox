using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared
{
    public enum BoundKeyState
    {
        Up,
        Down
    }

    /// <summary>
    /// Key Bindings - each corresponds to a logical function ingame.
    /// </summary>
    public enum BoundKeyFunctions
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        SwitchHands,
        Inventory,
        ShowFPS,
        Drop,
        Run,
    }
}
