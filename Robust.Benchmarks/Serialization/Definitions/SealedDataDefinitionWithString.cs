using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Benchmarks.Serialization.Definitions
{
    [DataDefinition]
    public sealed partial class SealedDataDefinitionWithString
    {
        [DataField("string")]
        public string StringField { get; private set; } = default!;
    }
}
