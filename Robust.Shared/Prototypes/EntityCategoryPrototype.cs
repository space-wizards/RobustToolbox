using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Prototype that represents game entities.
/// </summary>
[Prototype("entityCategory")]
public sealed class EntityCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name of the category, for use in entity spawn menus.
    /// </summary>
    [DataField("name")]
    public string? Name { get; private set; }

    /// <summary>
    /// Localized description of the category, for use in entity spawn menus.
    /// </summary>
    [DataField("description")]
    public string? Description { get; private set; }
}