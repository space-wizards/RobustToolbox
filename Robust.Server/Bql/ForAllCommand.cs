using System.Globalization;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Bql
{
    public sealed class ForAllCommand : LocalizedCommands
    {
        [Dependency] private readonly IBqlQueryManager _bql = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "forall";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var (entities, rest) = _bql.SimpleParseAndExecute(argStr[6..]);

            foreach (var ent in entities.ToList())
            {
                var cmds = SubstituteEntityDetails(_entities, shell, ent, rest).Split(";");
                foreach (var cmd in cmds)
                {
                    shell.ExecuteCommand(cmd);
                }
            }
        }

        // This will be refactored out soon.
        private static string SubstituteEntityDetails(
            IEntityManager entMan,
            IConsoleShell shell,
            EntityUid ent,
            string ruleString)
        {
            var transform = entMan.GetComponent<TransformComponent>(ent);
            var metadata = entMan.GetComponent<MetaDataComponent>(ent);

            // gross, is there a better way to do this?
            ruleString = ruleString.Replace("$ID", ent.ToString());
            ruleString = ruleString.Replace("$WX",
                transform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$WY",
                transform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LX",
                transform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LY",
                transform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$NAME", metadata.EntityName);

            if (shell.Player is IPlayerSession player)
            {
                var ptransform = player.AttachedEntityTransform;
                if (ptransform != null)
                {
                    ruleString = ruleString.Replace("$PID", ptransform.Owner.ToString());
                    ruleString = ruleString.Replace("$PWX",
                        ptransform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PWY",
                        ptransform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLX",
                        ptransform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLY",
                        ptransform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
                }
            }

            return ruleString;
        }
    }
}
