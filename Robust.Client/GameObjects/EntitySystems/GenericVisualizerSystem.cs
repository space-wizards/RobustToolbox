using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.GameObjects;

/// <summary>
///     A generic visualizer system that modifies sprite layer data.
/// </summary>
public sealed class GenericVisualizerSystem : VisualizerSystem<GenericVisualizerComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSys = default!;

    protected override void OnAppearanceChange(EntityUid uid, GenericVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var ent = new Entity<SpriteComponent?>(uid, args.Sprite);
        foreach (var (appearanceKey, layerDict) in component.Visuals)
        {
            if (!_appearanceSys.TryGetData(uid, appearanceKey, out object? obj, args.Component))
                continue;

            var appearanceValue = obj.ToString();
            if (string.IsNullOrEmpty(appearanceValue))
                continue;

            foreach (var (key, layerDataDict) in layerDict)
            {
                if (!layerDataDict.TryGetValue(appearanceValue, out var layerData))
                    continue;

                var index = _sprite.LayerMapReserve(ent, key);
                _sprite.LayerSetData(ent, index, layerData);
            }
        }
    }
}
