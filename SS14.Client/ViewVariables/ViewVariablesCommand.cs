using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Client.Interfaces.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.ViewVariables;

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
            if (args.Length == 0)
            {
                vvm.OpenVV(new Honk());
            }
            else
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var uid = EntityUid.Parse(args[0]);
                vvm.OpenVV(entityManager.GetEntity(uid));
            }
            return false;
        }
    }

    public class Honk : IEnumerable<object>
    {
        [ViewVariables(VVAccess.ReadWrite)] private int x = 10;

        [ViewVariables]
        public Dictionary<object, object> Dict => new Dictionary<object, object> {{"a", "b"}, {"c", "d"}};

        [ViewVariables]
        public List<object> List => new List<object> {1, 2, 3, 4, 5, 6, 7, 8, 9, x, 11, 12, 13, 14, 15};

        public IEnumerator<object> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
