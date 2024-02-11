using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Placement;

namespace Robust.Shared.Console.Commands;

public sealed class SpawnCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "spawn";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            shell.WriteError("Incorrect number of arguments. " + Help);
        }

        var pAE = shell.Player?.AttachedEntity ?? EntityUid.Invalid;

        PlacementEntityEvent? placementEv = null;

        if (args.Length == 1 && pAE != EntityUid.Invalid)
        {
            var entityCoordinates = _entityManager.GetComponent<TransformComponent>(pAE).Coordinates;
            var createdEntity = _entityManager.SpawnEntity(args[0], entityCoordinates);
            placementEv = new PlacementEntityEvent(createdEntity, entityCoordinates, PlacementEventAction.Create, shell.Player?.UserId);
        }
        else if (args.Length == 2)
        {
            var uidNet = NetEntity.Parse(args[1]);
            var entityCoordinates = _entityManager.GetComponent<TransformComponent>(_entityManager.GetEntity(uidNet)).Coordinates;
            var createdEntity = _entityManager.SpawnEntity(args[0], entityCoordinates);
            placementEv = new PlacementEntityEvent(createdEntity, entityCoordinates, PlacementEventAction.Create, shell.Player?.UserId);
        }
        else if (pAE != EntityUid.Invalid)
        {
            var coords = new MapCoordinates(
                float.Parse(args[1], CultureInfo.InvariantCulture),
                float.Parse(args[2], CultureInfo.InvariantCulture),
                _entityManager.GetComponent<TransformComponent>(pAE).MapID);

            var createdEntity = _entityManager.SpawnEntity(args[0], coords);
            placementEv = new PlacementEntityEvent(createdEntity, _entityManager.GetComponent<TransformComponent>(createdEntity).Coordinates, PlacementEventAction.Create, shell.Player?.UserId);
        }

        if (placementEv != null)
            _entityManager.EventBus.RaiseEvent(EventSource.Local, placementEv.Value);
    }
}
