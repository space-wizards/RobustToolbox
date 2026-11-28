using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Raised directed on the container BEFORE removing an entity.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="EntRemovedFromContainerMessage"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
/// <param name="Context">
/// Context explaining why or how this container action occurred.
/// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
/// </param>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that will be removed from the container.</param>
[ByRefEvent]
public readonly record struct EntRemovedFromContainerMessage<T>(T Context, BaseContainer Container, EntityUid Entity)
    where T : struct, IContainerActionContext;

/// <summary>
/// Raised directed on the container BEFORE removing an entity.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="EntRemovedFromContainerMessage{T}"/>.
/// </remarks>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that will be removed from the container.</param>
[ByRefEvent]
public readonly record struct EntRemovedFromContainerMessage(BaseContainer Container, EntityUid Entity);

/// <summary>
/// Raised directed on the entity BEFORE being removed from the container.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="EntGotRemovedFromContainerMessage"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
/// <param name="Context">
/// Context explaining why or how this container action occurred.
/// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
/// </param>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that will be removed from the container.</param>
[ByRefEvent]
public readonly record struct EntGotRemovedFromContainerMessage<T>(T Context, BaseContainer Container, EntityUid Entity)
    where T : struct, IContainerActionContext;

/// <summary>
/// Raised directed on the entity BEFORE being removed from the container.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="EntGotRemovedFromContainerMessage{T}"/>.
/// </remarks>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that will be removed from the container.</param>
[ByRefEvent]
public readonly record struct EntGotRemovedFromContainerMessage(BaseContainer Container, EntityUid Entity);
