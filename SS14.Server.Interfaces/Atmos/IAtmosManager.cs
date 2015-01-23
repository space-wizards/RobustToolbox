using Lidgren.Network;
using SS14.Shared;

namespace SS14.Server.Interfaces.Atmos
{
    public interface IAtmosManager
    {
        void InitializeGasCells();
        void Update(float frametime);
        void SendAtmosStateTo(NetConnection client);
        IGasProperties GetGasProperties(GasType g);
        void TotalAtmosReport();
        int NumGasTypes { get; }
    }
}