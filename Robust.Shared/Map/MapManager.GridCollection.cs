using System;
using Robust.Shared.Log;

// All the obsolete warnings about GridId are probably useless here.
#pragma warning disable CS0618

namespace Robust.Shared.Map;

internal partial class MapManager
{
    private GridId _highestGridId = GridId.Invalid;

    public GridId GenerateGridId(GridId? forcedGridId)
    {
        var actualId = forcedGridId ?? new GridId(_highestGridId.Value + 1);

        if(actualId == GridId.Invalid)
            throw new InvalidOperationException($"Cannot allocate a grid with an Invalid ID.");
        
        if (_highestGridId.Value < actualId.Value)
            _highestGridId = actualId;

        if(forcedGridId is not null) // this function basically just passes forced gridIds through.
            Logger.DebugS("map", $"Allocating new GridId {actualId}.");

        return actualId;
    }
}
