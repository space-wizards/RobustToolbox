using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Shared.EntitySerialization;

public static class SerializationTestHelper
{
    public static void LoadTileDefs(IPrototypeManager protoMan, ITileDefinitionManager tileMan, string? spaceId = "Space")
    {
        var prototypeList = new List<TileDef>();
        foreach (var tileDef in protoMan.EnumeratePrototypes<TileDef>())
        {
            if (tileDef.ID == spaceId)
            {
                // Filter out the space tile def and register it first
                tileMan.Register(tileDef);
                continue;
            }

            prototypeList.Add(tileDef);
        }

        prototypeList.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

        // Register the rest
        foreach (var tileDef in prototypeList)
        {
            tileMan.Register(tileDef);
        }
    }
}
