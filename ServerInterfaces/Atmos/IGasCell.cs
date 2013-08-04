using BKSystem.IO;
using SS13_Shared;
using ServerInterfaces.Map;

namespace ServerInterfaces.Atmos
{
    public interface IGasCell
    {
        Vector2 GasVelocity { get; }
        float TotalGas { get; }
        bool Calculated { get; }
        float Pressure { get; }
        void Update();
        void InitSTP();
        void CalculateNextGasAmount(IMapManager m);
        int PackDisplayBytes(BitStream bits, bool all = false);
        float GasAmount(GasType type);
        void AddGas(float amount, GasType gas);
    }
}