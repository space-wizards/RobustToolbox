using Robust.Shared.Containers;

namespace Robust.Server.Containers
{
    public class ContainerSystem : SharedContainerSystem
    {
        // Seems like shared EntitySystems aren't registered, so this is here to register it on the server.
        // Registering the SharedContainerSystem causes conflicts on client where two entity systems are registered.
    }
}
