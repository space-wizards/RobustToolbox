using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Benchmarks.Serialization
{
    [DataDefinition]
    public class DataDefinitionWithString
    {
        [field: DataField("string")]
        private string StringField { get; } = default!;
    }
}
