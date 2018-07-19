using SS14.Shared.Serialization;

namespace SS14.Shared.Interfaces.Serialization
{
    public interface IExposeData
    {
        void ExposeData(ObjectSerializer serializer);
    }
}
