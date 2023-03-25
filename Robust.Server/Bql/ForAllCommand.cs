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

            var transformSystem = _entities.System<SharedTransformSystem>();

            var (entities, rest) = _bql.SimpleParseAndExecute(argStr[6..]);

            foreach (var ent in entities.ToList())
            {
                var cmds = SubstituteEntityDetails(_entities, transformSystem, shell, ent, rest).Split(";");
                foreach (var cmd in cmds)
                {
                    shell.ExecuteCommand(cmd);
                }
            }
        }

        // This will be refactored out soon.
        private static string SubstituteEntityDetails(
            IEntityManager entMan,
            SharedTransformSystem transformSystem,
            IConsoleShell shell,
            EntityUid ent,
            string ruleString)
        {
            var transform = entMan.GetComponent<TransformComponent>(ent);
            var metadata = entMan.GetComponent<MetaDataComponent>(ent);

            var worldPos = transformSystem.GetWorldPosition(transform);
            var localPos = transform.LocalPosition;

            // gross, is there a better way to do this?
            ruleString = ruleString.Replace("$ID", ent.ToString());
            ruleString = ruleString.Replace("$WX", worldPos.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$WY", worldPos.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LX", localPos.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LY", localPos.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$NAME", metadata.EntityName);

            if (shell.Player is { AttachedEntity: { } pEntity})
            {
                var pTransform = entMan.GetComponent<TransformComponent>(pEntity);

                var pWorldPos = transformSystem.GetWorldPosition(pTransform);
                var pLocalPos = pTransform.LocalPosition;

                ruleString = ruleString.Replace("$PID", pEntity.ToString());
                ruleString = ruleString.Replace("$PWX", pWorldPos.X.ToString(CultureInfo.InvariantCulture));
                ruleString = ruleString.Replace("$PWY", pWorldPos.Y.ToString(CultureInfo.InvariantCulture));
                ruleString = ruleString.Replace("$PLX", pLocalPos.X.ToString(CultureInfo.InvariantCulture));
                ruleString = ruleString.Replace("$PLY", pLocalPos.Y.ToString(CultureInfo.InvariantCulture));
            }

            return ruleString;
        }
    }
}
