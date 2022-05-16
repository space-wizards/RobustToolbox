using System;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.UserInterface.Controls;

public sealed class GridList : FilteredItemList
{
    private readonly IMapManager _mapManager = default!;

    public event Action<IMapGrid?>? OnSelectionChanged;

    public Comparison<IMapGrid>? Comparison;
    private MapId? _mapId;

    /// <summary>
    /// Can be set to limit the GridList to only show grids from the specified map.
    /// </summary>
    public MapId? MapId
    {
        get => _mapId;
        set
        {
            if(value.HasValue && !_mapManager.MapExists(value.Value)) return;

            _mapId = value;
            PopulateList();
        }
    }

    public GridList()
    {
        IoCManager.InjectDependencies(this);
        ItemList.OnItemSelected += PlayerItemListOnOnItemSelected;
        ItemList.OnItemDeselected += PlayerItemListOnOnItemDeselected;
        PopulateList();
        _mapManager.OnGridCreated += PopulateList;
        _mapManager.OnGridRemoved += PopulateList;
    }


    private void PlayerItemListOnOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
    {
        var selectedGrid = (IMapGrid) obj.ItemList[obj.ItemIndex].Metadata!;
        OnSelectionChanged?.Invoke(selectedGrid);
    }

    private void PlayerItemListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
    {
        OnSelectionChanged?.Invoke(null);
    }

    private void PopulateList(MapId mapId = default, GridId gridId = default)
    {
        ItemList.Clear();

        foreach (var grid in _mapManager.GetAllGrids().OrderBy(grid => grid.Index.Value))
        {
            if(_mapId.HasValue && grid.ParentMapId != _mapId.Value)
                continue;

            var item = new ItemList.Item(ItemList)
            {
                Metadata = grid,
                Text = $"{grid.Index}: map: {grid.ParentMapId}, ent: {grid.GridEntityId}, pos: {grid.WorldPosition}"
            };
            ItemList.Add(item);
        }
    }
}
