using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
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
        AddComponent,
        BoundKeyChange,
        BoundKeyRepeat,
        MoveDirection,
        HealthStatus,
        SlaveAttach,
        ItemDetach,
    }
}
