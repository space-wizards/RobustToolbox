using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Client.MapEditor;

/// <summary>
/// Validate and fetch basic information about a map editor tool.
/// </summary>
/// <remarks>
/// This is used both to fetch basic information (tool name), and to validate that the tool is still valid
/// (e.g. if loaded from history, but a prototype has been removed since).
/// </remarks>
[ByRefEvent]
public struct MapEditorToolValidateEvent
{
    internal bool IsValid;
    public FormattedMessage? Name;

    public void MakeValid()
    {
        if (IsValid)
            throw new InvalidOperationException("Already valid!");

        if (Name is null)
            throw new InvalidOperationException("Cannot mark tool as valid without filling out name!");

        IsValid = true;
    }
}

[ByRefEvent]
public struct MapEditorToolMakePreviewControl
{
    internal Control? Control;

    public void SetControl(Control control)
    {
        if (Control != null)
            throw new InvalidOperationException("Already set!");

        Control = control;
    }
}

internal sealed partial class ClientMapEditorSystem
{
    private static readonly SerializationOptions ToolSerializationOptions = new()
    {
        Category = FileCategory.Entity
    };

    internal event Action<EntityUid, Entity<MapEditorToolDataComponent>?>? ActiveToolChanged;
    internal event Action<EntityUid, NotifyCollectionChangedEventArgs>? ToolHistoryChanged;

    public void SwitchToTool(EntityUid map, Action<Entity<MapEditorToolDataComponent>> ent)
    {
        var mapData = Comp<MapEditorClientMapDataComponent>(map);
        PushActiveToolToHistory((map, mapData));

        var newToolEnt = Spawn(null, new EntityCoordinates(map, Vector2.Zero));
        var toolData = AddComp<MapEditorToolDataComponent>(newToolEnt);
        MetaSys.SetEntityName(newToolEnt, "Tool entity");
        ent((newToolEnt, toolData));

        var validateEvent = new MapEditorToolValidateEvent();
        RaiseLocalEvent(newToolEnt, ref validateEvent);
        if (!validateEvent.IsValid)
        {
            Del(newToolEnt);
            throw new InvalidOperationException("Tool did not validate!");
        }

        DebugTools.Assert(validateEvent.Name != null);

        toolData.ToolName = validateEvent.Name;
        Log.Debug($"Selected tool: {validateEvent.Name}");

        mapData.ActiveToolEntity = newToolEnt;
        ActiveToolChanged?.Invoke(map, (newToolEnt, toolData));
    }

    private void PushActiveToolToHistory(Entity<MapEditorClientMapDataComponent> mapData)
    {
        if (mapData.Comp.ActiveToolEntity is not { } active)
            return;

        mapData.Comp.ToolEntityHistory.Insert(0, active);
        ToolHistoryChanged?.Invoke(
            mapData,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, active, 0));
        PruneToolEntityHistory(mapData);
        mapData.Comp.ActiveToolEntity = null;

        ActiveToolChanged?.Invoke(mapData, null);
    }

    private void PruneToolEntityHistory(Entity<MapEditorClientMapDataComponent> mapData)
    {
        var maxHistory = Configuration.GetCVar(MapEditorCVars.MaxToolHistory);
        if (mapData.Comp.ToolEntityHistory.Count <= maxHistory)
            return;

        var removed = new List<EntityUid>();
        for (var i = maxHistory; i < mapData.Comp.ToolEntityHistory.Count; i++)
        {
            var value = mapData.Comp.ToolEntityHistory[i];
            Del(value);
            removed.Add(value);
        }

        mapData.Comp.ToolEntityHistory.RemoveRange(maxHistory, mapData.Comp.ToolEntityHistory.Count - maxHistory);
        ToolHistoryChanged?.Invoke(mapData, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, maxHistory));
    }

    public Entity<MapEditorToolDataComponent>? GetActiveTool(EntityUid mapData)
    {
        var mapDataComp = Comp<MapEditorClientMapDataComponent>(mapData);
        if (mapDataComp.ActiveToolEntity is not { } active)
            return null;

        return (active, Comp<MapEditorToolDataComponent>(active));
    }
}
