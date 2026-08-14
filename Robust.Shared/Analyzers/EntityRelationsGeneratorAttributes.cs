using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Indicate that a <see cref="Component"/> should automatically handle setting entity relation fields to null when the related entity is deleted.
/// </summary>
/// <remarks>
///     When this attribute is set on a <see cref="Component"/>, an <see cref="EntitySystem"/> will automatically be
///     generated that increments any fields tagged with <see cref="AutoPausedFieldAttribute"/> when the entity is unpaused
///     (<see cref="EntityUnpausedEvent"/>).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class AutoGenerateEntityRelationsAttribute : Attribute
{
    /// <summary>
    ///     Whether the generated code should automatically call
    ///     <see cref="IEntityManager.Dirty(EntityUid,IComponent,MetaDataComponent)"/> after resetting the related entity.
    ///     This is automatically inferred for fields marked <see cref="AutoNetworkedFieldAttribute"/>.
    /// </summary>
    public bool Dirty = false;
}

/// <summary>
///     Mark a field or property to automatically handle setting an entity relation to null when the related entity is deleted.
/// </summary>
/// <remarks>
///     The type of the field must be <see cref="EntityRelation"/>,
///     an <see cref="IEnumerable{T}"/> listing EntityRelations,
///     or a Dictionary with EntityRelation as a key or a value.
///     For all other use cases handle the
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoRelationFieldAttribute : Attribute;
