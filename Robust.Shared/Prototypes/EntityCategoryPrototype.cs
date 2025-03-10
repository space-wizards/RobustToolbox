using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Prototype that represents some entity prototype category.
/// Useful for sorting or grouping entity prototypes for mapping/spawning UIs.
/// </summary>
[Prototype]
public sealed partial class EntityCategoryPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name of the category, for use in entity spawn menus.
    /// </summary>
    [DataField] public string? Name;

    /// <summary>
    /// Localized description of the category, for use in entity spawn menus.
    /// </summary>
    [DataField] public string? Description;

    /// <summary>
    /// Default suffix to give all entities that belong to this prototype.
    /// See <see cref="EntityPrototype.EditorSuffix"/>.
    /// </summary>
    [DataField] public string? Suffix;

    /// <summary>
    /// If true, any entity prototypes that belong to this category should not be shown in general entity spawning UIs.
    /// Useful for various entities that shouldn't be spawned directly.
    /// </summary>
    [DataField] public bool HideSpawnMenu;

    /// <summary>
    /// List of components that will cause an entity prototype to be automatically included in this category.
    /// </summary>
    [DataField(customTypeSerializer:typeof(CustomHashSetSerializer<string, ComponentNameSerializer>))]
    public HashSet<string>? Components;

    /// <summary>
    /// If true, then an entity prototype will automatically get added to this category if any of its parent belonged to
    /// it category.
    /// </summary>
    [DataField]
    public bool Inheritable = true;
}
