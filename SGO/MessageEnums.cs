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
        AddComponent,
        BoundKeyChange,
        BoundKeyRepeat,
        SlaveAttach,
    }
}
