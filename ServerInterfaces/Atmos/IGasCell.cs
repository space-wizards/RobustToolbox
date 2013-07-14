using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BKSystem.IO;
using SS13_Shared;
using ServerInterfaces.Map;
using ServerInterfaces.Tiles;

namespace ServerInterfaces.Atmos
{
    public interface IGasCell
    {
        void Update();
        void InitSTP();
        void CalculateNextGasAmount(IMapManager m);
        int PackDisplayBytes(BitStream bits, bool all = false);
        Vector2 GasVel { get; }
        float TotalGas { get; }
        float GasAmount(GasType type);
        void AddGas(float amount, GasType gas);
        bool Calculated { get; }
        
    }
}
