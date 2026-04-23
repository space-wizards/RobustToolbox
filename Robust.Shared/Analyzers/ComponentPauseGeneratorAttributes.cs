using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Indicate that a <see cref="Component"/> should automatically handle unpausing of timer fields.
/// </summary>
/// <remarks>
///     When this attribute is set on a <see cref="Component"/>, an <see cref="EntitySystem"/> will automatically be
///     generated that increments any fields tagged with <see cref="AutoPausedFieldAttribute"/> when the entity is unpaused
///     (<see cref="EntityUnpausedEvent"/>).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class AutoGenerateComponentPauseAttribute : Attribute
{
    /// <summary>
    ///     Whether the generated code should automatically call
    ///     <see cref="IEntityManager.Dirty(EntityUid,IComponent,MetaDataComponent)"/> after unpausing the entity.
    ///     This is automatically inferred for fields marked <see cref="AutoNetworkedFieldAttribute"/>.
    /// </summary>
    public bool Dirty = false;
}

/// <summary>
///     Mark a field or property to automatically handle unpausing with <see cref="AutoGenerateComponentPauseAttribute"/>.
/// </summary>
/// <remarks>
///     The type of the field or prototype must be <see cref="TimeSpan"/> (potentially nullable).
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoPausedFieldAttribute : Attribute;
