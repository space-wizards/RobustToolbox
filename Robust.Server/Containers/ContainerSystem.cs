using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Server.Containers
{
    public sealed class ContainerSystem : SharedContainerSystem
    {
#if DEBUG
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ContainerManagerComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, ContainerManagerComponent component, ComponentStartup args)
        {
            foreach (var cont in component.Containers.Values)
            {
                foreach (var ent in cont.ContainedEntities)
                {
                    DebugTools.Assert((MetaData(ent).Flags & MetaDataFlags.InContainer) != 0);
                }
            }
        }
    }
#endif
}
