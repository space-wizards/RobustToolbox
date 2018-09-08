using JetBrains.Annotations;
using SS14.Client.Interfaces.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.ViewVariables
{
    [UsedImplicitly]
    public class ViewVariablesCommand : IConsoleCommand
    {
        public string Command => "vv";
        public string Description => "Opens View Variables.";
        public string Help => "Usage: vv <entity ID>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var uid = EntityUid.Parse(args[0]);
            vvm.OpenVV(entityManager.GetEntity(uid));
            return false;
        }
    }
}
