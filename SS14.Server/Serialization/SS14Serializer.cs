using SS14.Server.Interfaces.Serialization;
using SS14.Shared.IoC;

namespace SS14.Server.Serialization
{
    [IoCTarget]
    public class SS14Serializer : SS14.Shared.Serialization.SS14Serializer, ISS14Serializer
    {
    }
}
