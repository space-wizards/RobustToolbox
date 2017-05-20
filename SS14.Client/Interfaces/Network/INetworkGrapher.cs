using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.Network
{
    public interface INetworkGrapher : IIoCInterface
    {
        void Update();
        void Toggle();
    }
}
