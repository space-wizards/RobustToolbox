using System.Collections.Generic;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace ServerInterfaces.GameObject
{
    public interface IGameObjectComponent: IComponent
    {
        void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection);
        void SetSVar(MarshalComponentParameter sVar);
        List<MarshalComponentParameter> GetSVars();
    }
}
