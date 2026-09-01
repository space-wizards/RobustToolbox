using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Indicate that a <see cref="Component"/> should automatically handle setting entity relation fields to null when the related entity is deleted.
/// </summary>
/// <remarks>
/// When this attribute is set on a <see cref="Component"/>, an <see cref="EntitySystem"/> will automatically be
/// generated that clears any relation fields tagged with <see cref="AutoRelationFieldAttribute"/> when the entity stored inside is deleted
/// (<see cref="EntityRelationDeleteEvent"/>).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class AutoGenerateEntityRelationsAttribute(bool dirty = false, bool shutdownEvent = true) : Attribute
{
    /// <summary>
    /// Whether the generated code should automatically call
    /// <see cref="IEntityManager.Dirty(EntityUid,IComponent,MetaDataComponent)"/> after resetting the related entity.
    /// This is automatically inferred for fields marked <see cref="AutoNetworkedFieldAttribute"/>.
    /// </summary>
    /// <remarks>
    /// This is useful for custom component network handling in order to properly send a new state
    /// when the related entity is deleted.
    /// </remarks>
    public readonly bool Dirty = dirty;

    /// <summary>
    /// Whether the generated code should subscribe to the <see cref="ComponentShutdown"/> event.
    /// in order to clear the related links.
    /// </summary>
    public readonly bool ShutdownEvent = shutdownEvent;
}

/// <summary>
/// Mark a field or property to automatically handle setting an entity relation to null when the related entity is deleted.
/// </summary>
/// <remarks>
/// The type of the field must be <see cref="EntityRelation"/>, <see cref="Nullable{EntityRelation}"/>,
/// <see cref="List{EntityRelation}"/>, <see cref="HashSet{EntityRelation}"/>,
/// or a Dictionary with EntityRelation as a key or a value.
/// For all other use cases handle the relations deletion manually using
/// <see cref="EntityRelationDeleteEvent"/>, <see cref="ComponentShutdown"/> and <see cref="EntityRelationShutdownEvent"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoRelationFieldAttribute : Attribute;
