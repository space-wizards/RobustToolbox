using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Benchmarks.Serialization.Definitions
{
    [DataDefinition]
    public sealed class SealedDataDefinitionWithString
    {
        [DataField("string")]
        public string StringField { get; init; } = default!;
    }
}
