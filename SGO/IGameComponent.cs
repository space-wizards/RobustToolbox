using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    interface IGameObjectComponent
    {
        Entity Owner { get; set; }

        void RecieveMessage(MessageType type, params object[] list);
    }
}
