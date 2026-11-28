using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Raised directed on the container AFTER inserting an entity.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="EntInsertedIntoContainerMessage"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
/// <param name="Context">
/// Context explaining why or how this container action occurred.
/// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
/// </param>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that was inserted the container.</param>
/// <param name="OldParent">The old parent of the inserted entity.</param>
[ByRefEvent]
public readonly record struct EntInsertedIntoContainerMessage<T>(T Context, BaseContainer Container, EntityUid Entity, EntityUid OldParent)
    where T : struct, IContainerActionContext;

/// <summary>
/// Raised directed on the container AFTER inserting an entity.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="EntInsertedIntoContainerMessage{T}"/>.
/// </remarks>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that was inserted the container.</param>
/// <param name="OldParent">The old parent of the inserted entity.</param>
[ByRefEvent]
public readonly record struct EntInsertedIntoContainerMessage(BaseContainer Container, EntityUid Entity, EntityUid OldParent);

/// <summary>
/// Raised directed on the entity AFTER being inserted into the container.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="EntGotInsertedIntoContainerMessage"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
/// <param name="Context">
/// Context explaining why or how this container action occurred.
/// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
/// </param>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that was inserted the container.</param>
/// <param name="OldParent">The old parent of the inserted entity.</param>
[ByRefEvent]
public readonly record struct EntGotInsertedIntoContainerMessage<T>(T Context, BaseContainer Container, EntityUid Entity, EntityUid OldParent)
    where T : struct, IContainerActionContext;

/// <summary>
/// Raised directed on the entity AFTER being inserted into the container.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="EntGotInsertedIntoContainerMessage{T}"/>.
/// </remarks>
/// <param name="Container">The container being acted upon.</param>
/// <param name="Entity">The entity that was inserted the container.</param>
/// <param name="OldParent">The old parent of the inserted entity.</param>
[ByRefEvent]
public readonly record struct EntGotInsertedIntoContainerMessage(BaseContainer Container, EntityUid Entity, EntityUid OldParent);
