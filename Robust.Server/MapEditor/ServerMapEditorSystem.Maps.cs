using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.MapEditor;
using Robust.Shared.Utility;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using GState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorGlobalStateComponent>;
using UState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorUserStateComponent>;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = null!;

    private void HandleCreateMap(MEM.CreateNewMap msg, GState gState, UState uState)
    {
        var map = _mapSystem.CreateMap(runMapInit: false);
        InitializeMapForUser(gState, uState, map, GetUntitledMapName(gState));
    }

    private string GetUntitledMapName(GState state)
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

    private void HandleOpenMap(MEM.OpenMap msg, GState gState, UState uState, EntitySessionEventArgs args)
    {
        var ms = new MemoryStream(msg.Data, writable: false);
        using var decompression = new ZStdDecompressStream(ms);

        var name = msg.Name ?? GetUntitledMapName(gState);

        var loaderOptions = new MapLoadOptions
        {
            DeserializationOptions = new DeserializationOptions
            {
                LogOrphanedGrids = false,
            },
        };

        if (!_mapLoader.TryLoadGeneric(decompression, name, out var result, loaderOptions))
        {
            RaiseNetworkEvent(new MEM.OpenMapFailed
                {
                    Name = name,
                    Handle = msg.Handle,
                },
                args.SenderSession);

            return;
        }

        if (result.Maps.Count > 1)
            throw new NotImplementedException("Multi-map file loading not yet supported");

        var mapData = InitializeMapForUser(gState, uState, result.Maps.Single(), name);
        if (msg.Handle != null)
        {
            mapData.Comp.FileHandles.Add(args.SenderSession.UserId, msg.Handle.Value);
            Dirty(mapData);
        }
    }

    private Entity<MapEditorMapDataComponent> InitializeMapForUser(
        GState gState,
        UState uState,
        EntityUid map,
        string name)
    {
        var mapDataEnt = Spawn(null, new EntityCoordinates(gState, default));
        var mapData = AddComp<MapEditorMapDataComponent>(mapDataEnt);
        mapData.MapEntity = map;
        _metaSys.SetEntityName(mapDataEnt, name);
        Dirty(mapDataEnt, mapData);

        gState.Comp.Maps.Add(mapDataEnt);
        Dirty(gState);

        // Add map to user's list of open maps so they can immediately see it.
        uState.Comp.OpenMaps.Add(mapDataEnt);
        Dirty(uState);

        return (mapDataEnt, mapData);
    }

    private void HandleSaveMap(MEM.SaveMap msg, EntitySessionEventArgs args)
    {
        // TODO: Saving is really important. Report proper errors to user if it fails for any reason.

        if (!TryGetEntity(msg.MapData, out var map))
            return;

        var mapData = Comp<MapEditorMapDataComponent>(map.Value);
        mapData.FileHandles[args.SenderSession.UserId] = msg.Handle;

        Dirty(map.Value, mapData);

        // TODO: Proper grid saving


        var (node, _) = _mapLoader.SerializeEntitiesRecursive([mapData.MapEntity],
            new SerializationOptions
            {
                Category = FileCategory.Map,
            });

        // TODO: make public API to save to stream.

        var ms = new MemoryStream();
        using (var compress = new ZStdCompressStream(ms))
        {
            MapLoaderSystem.Write(compress, node);
        }

        RaiseNetworkEvent(new MEM.SaveMapData
        {
            Handle = msg.Handle,
            MapData = msg.MapData,
            Data = ms.ToArray()
        }, args.SenderSession);
    }
}
