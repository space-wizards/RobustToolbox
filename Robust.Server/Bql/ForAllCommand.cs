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
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var (entities, rest) = queryManager.SimpleParseAndExecute(argStr[6..]);

            foreach (var ent in entities.ToList())
            {
                var cmds = SubstituteEntityDetails(shell, entityManager.GetEntity(ent), rest).Split(";");
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
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent.Uid).WorldPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$WY",
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent.Uid).WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LX",
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent.Uid).LocalPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LY",
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent.Uid).LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$NAME", IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ent.Uid).EntityName);

            if (shell.Player is IPlayerSession player)
            {
                if (player.AttachedEntity != null)
                {
                    var p = player.AttachedEntity;
                    ruleString = ruleString.Replace("$PID", ent.Uid.ToString());
                    ruleString = ruleString.Replace("$PWX",
                        IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(p.Uid).WorldPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PWY",
                        IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(p.Uid).WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLX",
                        IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(p.Uid).LocalPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLY",
                        IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(p.Uid).LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
                }
            }

            return ruleString;
        }
    }
}
