using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerInterfaces.Player;

namespace ServerInterfaces.GOC
{
    public interface IActorComponent
    {
        IPlayerSession GetPlayerSession();
    }
}
