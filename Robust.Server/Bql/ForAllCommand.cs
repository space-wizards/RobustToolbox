using System.Globalization;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Bql
{
    public class ForAllCommand : IConsoleCommand
    {
        public string Command => "forall";
        public string Description => "Runs a command over all entities with a given component";
        public string Help => "Usage: forall <bql query> do <command...>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var queryManager = IoCManager.Resolve<IBqlQueryManager>();
            var (entities, rest) = queryManager.SimpleParseAndExecute(argStr[6..]);

            foreach (var ent in entities.ToList())
            {
                var cmds = SubstituteEntityDetails(shell, ent, rest).Split(";");
                foreach (var cmd in cmds)
                {
                    shell.ExecuteCommand(cmd);
                }
            }
        }

        // This will be refactored out soon.
        private static string SubstituteEntityDetails(IConsoleShell shell, IEntity ent, string ruleString)
        {
            // gross, is there a better way to do this?
            ruleString = ruleString.Replace("$ID", ent.Uid.ToString());
            ruleString = ruleString.Replace("$WX",
                ent.Transform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$WY",
                ent.Transform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LX",
                ent.Transform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LY",
                ent.Transform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$NAME", ent.Name);

            if (shell.Player is IPlayerSession player)
            {
                if (player.AttachedEntity != null)
                {
                    var p = player.AttachedEntity;
                    ruleString = ruleString.Replace("$PID", ent.Uid.ToString());
                    ruleString = ruleString.Replace("$PWX",
                        p.Transform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PWY",
                        p.Transform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLX",
                        p.Transform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLY",
                        p.Transform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
                }
            }

            return ruleString;
        }
    }
}
