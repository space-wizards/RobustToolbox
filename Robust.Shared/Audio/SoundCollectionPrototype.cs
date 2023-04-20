using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Robust.Shared.Audio;

[Prototype("soundCollection")]
public sealed class SoundCollectionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("files")]
    public List<ResourcePath> PickFiles { get; } = new();
}
