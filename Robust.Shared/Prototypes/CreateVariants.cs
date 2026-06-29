using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Prototypes;

[DataDefinition]
public sealed partial class CreateVariants
{
    [VariantValuesField]
    public string[]? Values;

    [VariantSequencesField]
    public string[][]? Sequences;
}

public sealed class VariantValuesFieldAttribute : DataFieldAttribute
{
    public const string Name = "values";
    public VariantValuesFieldAttribute(int priority = 1) :
        base(Name, false, priority, false, false)
    {
    }
}

public sealed class VariantSequencesFieldAttribute : DataFieldAttribute
{
    public const string Name = "sequences";
    public VariantSequencesFieldAttribute(int priority = 1) :
        base(Name, false, priority, false, false)
    {
    }
}
