using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Robust.Client.GameObjects;

/// <summary>
///     This component can be used to apply generic changes to an entity's sprite component as a result of appearance
///     data changes.
/// </summary>
[RegisterComponent]
[Access(typeof(GenericVisualizerSystem))]
public sealed class GenericVisualizerComponent : Component
{
    /// <summary>
    ///     This is a nested dictionary that maps appearance data keys -> sprite layer keys -> appearance data values -> layer data.
    ///     While somewhat convoluted, this enables the sprite layer data to be completely modified using only yaml.
    ///
    ///     In most instances, each of these dictionaries will probably only have a single entry.
    /// </summary>
    [DataField("visuals", required:true)]
    public Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> Visuals = default!;
}
