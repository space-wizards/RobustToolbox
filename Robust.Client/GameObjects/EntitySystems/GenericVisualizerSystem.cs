using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.GameObjects;

/// <summary>
///     A generic visualizer system that modifies sprite layer data.
/// </summary>
public sealed class GenericVisualizerSystem : VisualizerSystem<GenericVisualizerComponent>
{
    [Dependency] private readonly IReflectionManager _refMan = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSys = default!;

    protected override void OnAppearanceChange(EntityUid uid, GenericVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        foreach (var (appearanceKey, layerDict) in component.Visuals)
        {
            if (!_appearanceSys.TryGetData(uid, appearanceKey, out var appearanceValue, args.Component))
                continue;

            var valueString = appearanceValue.ToString();
            if (string.IsNullOrEmpty(valueString))
                continue;

            foreach (var (layerKeyRaw, layerDataDict) in layerDict)
            {
                if (!layerDataDict.TryGetValue(valueString, out var layerData))
                    continue;

                object layerKey = _refMan.TryParseEnumReference(layerKeyRaw, out var @enum)
                    ? @enum
                    : layerKeyRaw;

                var layerIndex = args.Sprite.LayerMapReserveBlank(layerKey);
                args.Sprite.LayerSetData(layerIndex, layerData);
            }
        }
    }
}
