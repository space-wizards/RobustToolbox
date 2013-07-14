using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using Lidgren.Network;

namespace ServerInterfaces.Atmos
{
    public interface IAtmosManager
    {
        void InitializeGasCells();
        void Update();
        void SendAtmosStateTo(NetConnection client);
    }
}
