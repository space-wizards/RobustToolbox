using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Benchmarks.Serialization.Definitions
{
    [DataDefinition]
    [Virtual]
    public class DataDefinitionWithString
    {
        [DataField("string")]
        public string StringField { get; init; } = default!;
    }
}
