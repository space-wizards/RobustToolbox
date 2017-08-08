using System.IO;

namespace SS14.Shared.Interfaces.Serialization
{
    public interface ISS14Serializer
    {
        void Initialize();
        void Serialize(Stream stream, object toSerialize);
        T Deserialize<T>(Stream stream);
        object Deserialize(Stream stream);
    }
}
