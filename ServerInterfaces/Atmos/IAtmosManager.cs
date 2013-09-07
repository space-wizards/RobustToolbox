using Lidgren.Network;
using SS13_Shared;

namespace ServerInterfaces.Atmos
{
    public interface IAtmosManager
    {
        void InitializeGasCells();
        void Update(float frametime);
        void SendAtmosStateTo(NetConnection client);
        IGasProperties GetGasProperties(GasType g);
        void TotalAtmosReport();
    }
}