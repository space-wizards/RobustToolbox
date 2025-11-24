using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.MapEditor;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using EState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorStateComponent>;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = null!;

    private void HandleCreateMap(MEM.CreateNewMap msg, EntitySessionEventArgs args)
    {
        if (!CommandCheck(args, msg, out var gState, out var uState))
            return;

        var map = _mapSystem.CreateMap(runMapInit: false);

        var mapDataEnt = Spawn(null, new EntityCoordinates(gState, default));
        var mapData = AddComp<MapEditorMapDataComponent>(mapDataEnt);
        mapData.MapEntity = map;
        _metaSys.SetEntityName(mapDataEnt, GetUntitledMapName(gState));
        Dirty(mapDataEnt, mapData);

        gState.Comp.Maps.Add(mapDataEnt);
        Dirty(gState);

        // Add map to user's list of open maps so they can immediately see it.
        uState.Comp.OpenMaps.Add(mapDataEnt);
        Dirty(uState);
    }

    private string GetUntitledMapName(EState state)
    {
        var allNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var mapUid in state.Comp.Maps)
        {
            allNames.Add(Name(mapUid));
        }

        string name;
        var i = 1;
        do
        {
            name = $"Untitled {i}";
            i += 1;
        } while (allNames.Contains(name));

        return name;
    }
}
