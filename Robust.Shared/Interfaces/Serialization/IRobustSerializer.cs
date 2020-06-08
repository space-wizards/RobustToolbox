using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.Interfaces.Network;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IRobustSerializer
    {
        void Initialize();
        void Serialize(Stream stream, object toSerialize);
        T Deserialize<T>(Stream stream);
        object Deserialize(Stream stream);
        bool CanSerialize(Type type);

        Task Handshake(INetChannel sender);

        event Action ClientHandshakeComplete;
    }
}
