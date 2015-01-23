using SS14.Shared;

namespace SS14.Server.Interfaces.Atmos
{
    public interface IGasProperties
    {
        string Name { get; }
        float SpecificHeatCapacity { get; }
        float MolecularMass { get; }
        GasType Type { get; }
        bool Combustable { get; }
        bool Oxidant { get; }
        float AutoignitionTemperature { get; }
    }
}