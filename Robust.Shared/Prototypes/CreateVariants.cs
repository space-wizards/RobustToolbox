using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using System.Collections.Generic;

namespace Robust.Shared.Prototypes;

[DataDefinition]
public sealed partial class CreateVariants
{
    [VariantValuesField]
    public string[]? Values;

    [VariantSequencesField]
    public string[][]? Sequences;

    [VariantMapsField]
    public Dictionary<string, DataNode>[]? Maps;
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

public sealed class VariantMapsFieldAttribute : DataFieldAttribute
{
    public const string Name = "maps";
    public VariantMapsFieldAttribute(int priority = 1) :
        base(Name, false, priority, false, false)
    {
    }
}
