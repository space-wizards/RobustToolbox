using JetBrains.Annotations;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    public class ReloadCommand: IClientCommand
    {
        public string Command => "reload";

        public string Description => "Reloads all entity prototypes and updates entities in-game accordingly";

        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // Clear all prototypes
            prototypeManager.Clear();
            prototypeManager.ReloadPrototypeTypes();
            prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes/"));
            prototypeManager.Resync();

            foreach (var prototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                foreach (var entity in entityManager.GetEntities(new PredicateEntityQuery(e => e.Prototype != null && e.Prototype.ID == prototype.ID)))
                {
                    prototype.UpdateEntity(entity as Entity);
                }
            }
        }
    }
}
