using System.IO;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IRobustSerializer
    {
        void Initialize();
        void Serialize(Stream stream, object toSerialize);
        T Deserialize<T>(Stream stream);
        object Deserialize(Stream stream);
    }
}
