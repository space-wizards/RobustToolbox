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
        float MolecularMass { get; }
        GasType Type { get; }
        bool Combustable { get; }
        bool Oxidant { get; }
        float AutoignitionTemperature { get; }
    }
}
