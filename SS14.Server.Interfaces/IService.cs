using SS14.Shared;

namespace SS14.Server.Interfaces
{
    public interface IService
    {
        ServerServiceType ServiceType { get; }
    }
}