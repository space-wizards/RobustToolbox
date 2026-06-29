using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Prototypes;

/// <summary>
/// This class is part of a mechanism to automatically generate variants of a prototype at run time.
/// It is substituted in the place of a value or collection in a YAML prototype definition.
/// When the prototype manager reads this class, it will use the arrays defined here to dynamically populate the associated value/collection.
/// To generate prototype variants, the ID of the prototype must also be replaced with a reference to this class.
/// </summary>
/// <example>
/// This following code will generate three different prototypes with differing values at runtime:
///
/// - type: entity
///   id: !type:CreateVariants
///     variants: [ TestEntityA, TestEntityB, TestEntityC ]
///   components:
///   - type: Test
///     floatValue: !type:CreateVariants
///       values: [ 0.5, 1, 1.5 ]
///     enumValue: !type:CreateVariants
///       values: [ 1, 2, 3 ]
///     stringArray: !type:CreateVariants
///       sequences: [ [ string-1 ], [ string-1, string-2 ], [ string-1, string-2, string-3 ] ]
///   - type: Sprite
///     sprite: path/to/overlay.rsi
///     layers:
///     - sprite: !type:CreateVariants
///         values: [ path/to/base.rsi, path/to/base_alt1.rsi, path/to/base_alt2.rsi ]
///       state: baseState
///       map: ["firstLayer"]
///     - state: overlayState
///       map: ["secondLayer"]
///
/// For example, the second prototype to be generated will have the following values:
///
/// - type: entity
///   id: TestEntityB
///   components:
///   - type: Test
///     floatValue: 1
///     enumValue: 2
///     stringArray: [ string-1, string-2 ]
///   - type: Sprite
///     sprite: path/to/overlay.rsi
///     layers:
///     - sprite: path/to/base_alt1.rsi
///       state: baseState
///       map: ["firstLayer"]
///     - state: overlayState
///       map: ["secondLayer"]
/// </example>
[DataDefinition]
public sealed partial class CreateVariants
{
    /// <summary>
    /// Use this field when replacing a single value (e.g., a number or string) in a prototype at runtime.
    /// </summary>
    [VariantValuesField]
    public string[]? Values;

    /// <summary>
    /// Use this field when replacing a collection of values (e.g., an array) in a prototype at runtime.
    /// </summary>
    [VariantSequencesField]
    public string[][]? Sequences;
}

/// <summary>
/// Denotes an array that will be used to populate variants of a prototype at runtime.
/// </summary>
public sealed class VariantValuesFieldAttribute : DataFieldAttribute
{
    public const string Name = "values";

    /// <summary>
    /// Denotes an array that will be used to populate variants of a prototype at runtime.
    /// </summary>
    /// <param name="priority">See <see cref="DataFieldBaseAttribute.Priority"/>.</param>
    public VariantValuesFieldAttribute(int priority = 1) :
        base(Name, false, priority, false, false)
    {
    }
}

/// <summary>
/// Denotes an array that will be used to populate variants of a prototype at runtime.
/// </summary>
public sealed class VariantSequencesFieldAttribute : DataFieldAttribute
{
    public const string Name = "sequences";

    /// <summary>
    /// Denotes an array that will be used to populate variants of a prototype at runtime.
    /// </summary>
    /// <param name="priority">See <see cref="DataFieldBaseAttribute.Priority"/>.</param>
    public VariantSequencesFieldAttribute(int priority = 1) :
        base(Name, false, priority, false, false)
    {
    }
}
