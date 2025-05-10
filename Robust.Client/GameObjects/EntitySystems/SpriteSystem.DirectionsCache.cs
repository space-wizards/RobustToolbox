
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem : EntitySystem
{
    public void UpdateOrientationCache(SpriteComponent component)
    {
        // this happened cause of building all in one method
        if (component.Layers.Count == 0 || (component._directionOrderUnparsed.Count == 0 && component._directionOrder.Count == 0))
            return;
        // this should be in init/after deserialization

        component._directionOrder.Clear();
        component.CachedDirectionLayerOrder.Clear();
        foreach (var (directionKey, unparsedOrder) in component._directionOrderUnparsed)
        {
            //TODO: move to component with other ParseKey calls
            List<object> parsedOrder = new(unparsedOrder.Count);
            foreach(var unparsed in unparsedOrder)
            {
                parsedOrder.Add(component.ParseKey(unparsed));
            }
            parsedOrder.TrimExcess();
            component._directionOrder.Add(directionKey, parsedOrder);
        }

        foreach (var layer in component.Layers)
        {
            component.RsiDirectionTypes |= ((byte?)layer.ActualState?.RsiDirections) ?? (byte)RsiDirectionType.Dir1;
        }

        // parameters pre-process
        // It should not be here. Only if its updated then to sort
        // foreach (var (_, value) in _directionOrder)
        // {
        //     value.Sort();
        // }

        Dictionary<RsiDirection, List<int>> tempUnorderedLayers = new();
        Dictionary<RsiDirection, SortedList<int, int>> tempOrderedLayers = new ();

        foreach (var (mappedDirection,_) in component._directionOrder)
        {
            List<int> unorderedLayers = new(component.Layers.Count);
            SortedList<int, int> orderedLayers = new(component.Layers.Count);

            tempUnorderedLayers.Add(mappedDirection, unorderedLayers);
            tempOrderedLayers.Add(mappedDirection, orderedLayers);
        }

        // Make cache section
        // original order was by Layers, but they were overridden exactly by LayerMap, so...
        // I need to double check how it worked
        foreach (var (map, layerIndex) in component.LayerMap)
        {
            var layer = component.Layers[layerIndex];
            // that could be removed but I need to think about it
            component.MaxRsiDirectionType = layer.ActualState?.RsiDirections > component.MaxRsiDirectionType ?
                                    layer.ActualState.RsiDirections : component.MaxRsiDirectionType;

            DebugTools.Assert(layer is not null);

            foreach (var iterDirection in Enum.GetValues<RsiDirection>())
            {
                // thats THE disaster
                if ((component.RsiDirectionTypes & (byte)RsiDirectionType.Dir8) == 0 && iterDirection > RsiDirection.West)
                    continue;

                var direction = iterDirection.OffsetRsiDir(layer.DirOffset);

                // it work bad at strings... needs cheking

                int orderedIndex = component._directionOrder.TryGetValue(direction, out var objectOrder) ?
                                    objectOrder.FindIndex(map.Equals) : -1;
                                    // objectOrder.BinarySearch(map) : -1;


                if (orderedIndex < 0)
                {
                    tempUnorderedLayers[direction].Add(layerIndex);
                }
                else
                {
                    // Logger.Debug($"Found match in {orderedIndex} layer map is {map} order map is {objectOrder?[orderedIndex]}.");
                    tempOrderedLayers[direction].Add(orderedIndex ,layerIndex);
                }
            }
        }

        // SaveCache section
        foreach (var (mappedDirection, orderedLayers) in tempOrderedLayers)
        {

#if DEBUG
        // I need to double check that it is correct, just to make myself calm
            int valIndex = 0;
            int prevKey = -1;
            foreach (var (key, value) in orderedLayers)
            {
                DebugTools.AssertEqual(value, orderedLayers.Values.ElementAt(valIndex));
                DebugTools.Assert(prevKey < key);
                valIndex++;
                prevKey = key;
            }
#endif
        DebugTools.Assert(tempUnorderedLayers[mappedDirection] is not null);
        List<int> resultList = new(component.Layers.Count);

        if (component._unorderedFirst)
        {
            resultList.AddRange(tempUnorderedLayers[mappedDirection]);
            resultList.AddRange(orderedLayers.Values);
        }
        else
        {
            resultList.AddRange(orderedLayers.Values);
            resultList.AddRange(tempUnorderedLayers[mappedDirection]);
        }

        resultList.TrimExcess();
        component.CachedDirectionLayerOrder.Add(mappedDirection, resultList);
        }

        // will it clear lists inside? Do I even need to think of it?
        tempOrderedLayers.Clear();
        tempUnorderedLayers.Clear();
    }
}

