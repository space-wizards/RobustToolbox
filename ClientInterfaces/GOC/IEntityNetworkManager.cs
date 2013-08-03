using GameObject;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IEntityNetworkManager: GameObject.IEntityNetworkManager
    {
        /// <summary>
        /// Sends an SVar to the server to be set on the server-side entity.
        /// </summary>
        /// <param name="sendingEntity"></param>
        /// <param name="svar"></param>
        void SendSVar(Entity sendingEntity, MarshalComponentParameter svar);
    }
}