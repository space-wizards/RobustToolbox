using System;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.UserInterface.Controls;

public sealed class MapList : FilteredItemList
{
    private readonly IMapManager _mapManager = default!;

    public event Action<MapId?>? OnSelectionChanged;

    public Comparison<MapId>? Comparison;

    public MapList()
    {
        IoCManager.InjectDependencies(this);
        ItemList.OnItemSelected += PlayerItemListOnOnItemSelected;
        ItemList.OnItemDeselected += PlayerItemListOnOnItemDeselected;
        PopulateList();
        _mapManager.MapCreated += PopulateList;
        _mapManager.MapDestroyed += PopulateList;
    }

    private void PlayerItemListOnOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
    {
        var selectedMap = (MapId) obj.ItemList[obj.ItemIndex].Metadata!;
        OnSelectionChanged?.Invoke(selectedMap);
    }

    private void PlayerItemListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
    {
        OnSelectionChanged?.Invoke(null);
    }

    private void PopulateList(object? obj = null, MapEventArgs? args = null)
    {
        ItemList.Clear();

        foreach (var mapId in _mapManager.GetAllMapIds())
        {
            var item = new ItemList.Item(ItemList)
            {
                Metadata = mapId,
                Text = $"map: {mapId}"
            };
            ItemList.Add(item);
        }
    }
}
