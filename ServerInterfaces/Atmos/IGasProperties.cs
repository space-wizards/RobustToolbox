using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerInterfaces.Atmos
{
    public interface IGasProperties
    {
        string Name { get;}
        float SpecificHeatCapacity { get;}
        GasType Type { get; }
    }
}
