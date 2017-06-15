using SS14.Client.Interfaces.Serialization;
using SS14.Shared.IoC;

namespace SS14.Client.Serialization
{
    [IoCTarget]
    public class SS14Serializer : SS14.Shared.Serialization.SS14Serializer, ISS14Serializer
    {
    }
}
