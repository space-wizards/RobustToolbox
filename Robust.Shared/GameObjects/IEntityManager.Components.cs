using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public partial interface IEntityManager
    {
        /// <summary>
        ///     A component was added to the manager.
        /// </summary>
        event Action<AddedComponentEventArgs>? ComponentAdded;

        /// <summary>
        ///     A component was removed from the manager.
        /// </summary>
        event Action<RemovedComponentEventArgs>? ComponentRemoved;

        /// <summary>
        ///     Calls Initialize() on all registered components of the entity.
        /// </summary>
        [Obsolete("Use InitializeEntity")]
        void InitializeComponents(EntityUid uid, MetaDataComponent? meta = null);

        /// <summary>
        ///     Calls Startup() on all registered components of the entity.
        /// </summary>
        [Obsolete("Use StartEntity")]
        void StartComponents(EntityUid uid);

        /// <summary>
        /// Gets the number of a specific component.
        /// </summary>
        public int Count<T>() where T : IComponent;

        /// <summary>
        /// Gets the number of a specific component.
        /// </summary>
        int Count(Type component);

        /// <summary>
        /// Adds the specified components from the <see cref="EntityPrototype"/>
        /// </summary>
        void AddComponents(EntityUid target, EntityPrototype prototype, bool removeExisting = true);

        /// <summary>
        /// Adds the specified registry components to the target entity.
        /// </summary>
        void AddComponents(EntityUid target, ComponentRegistry registry, bool removeExisting = true);

        /// <summary>
        /// Removes the specified entity prototype components from the target entity.
        /// </summary>
        void RemoveComponents(EntityUid target, EntityPrototype prototype);

        /// <summary>
        /// Removes the specified registry components from the target entity.
        /// </summary>
        void RemoveComponents(EntityUid target, ComponentRegistry registry);

        /// <summary>
        ///     Adds a Component type to an entity. If the entity is already Initialized, the component will
        ///     automatically be Initialized and Started.
        /// </summary>
        /// <typeparam name="T">Concrete component type to add.</typeparam>
        /// <returns>The newly added component.</returns>
        T AddComponent<T>(EntityUid uid) where T : IComponent, new();

        /// <summary>
        ///     Adds a Component with a given network id to an entity.
        /// </summary>
        IComponent AddComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null);

        /// <summary>
        ///     Adds a Component to an entity. If the entity is already Initialized, the component will
        ///     automatically be Initialized and Started.
        /// </summary>
        /// <param name="uid">Entity being modified.</param>
        /// <param name="component">Component to add.</param>
        /// <param name="overwrite">Should it overwrite existing components?</param>
        void AddComponent<T>(EntityUid uid, T component, bool overwrite = false, MetaDataComponent? metadata = null) where T : IComponent;

        /// <summary>
        ///     Removes the component with the specified reference type,
        ///     Without needing to have the component itself.
        /// </summary>
        /// <typeparam name="T">The component reference type to remove.</typeparam>
        /// <param name="uid">Entity UID to modify.</param>
        bool RemoveComponent<T>(EntityUid uid, MetaDataComponent? meta = null) where T : IComponent;

        /// <summary>
        ///     Removes the component with a specified type.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>Returns false if the entity did not have the specified component.</returns>
        bool RemoveComponent(EntityUid uid, Type type, MetaDataComponent? meta = null);

        /// <summary>
        ///     Removes the component with a specified network ID.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="netID">Network ID of the component to remove.</param>
        /// <returns>Returns false if the entity did not have the specified component.</returns>
        bool RemoveComponent(EntityUid uid, ushort netID, MetaDataComponent? meta = null);

        /// <summary>
        ///     Removes the specified component. Throws if the given component does not belong to the entity.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="component">Component to remove.</param>
        void RemoveComponent(EntityUid uid, IComponent component, MetaDataComponent? meta = null);

        /// <summary>
        ///     Immediately shuts down a component, but defers the removal and deletion until the end of the tick.
        ///     Without needing to have the component itself.
        /// </summary>
        /// <typeparam name="T">The component reference type to remove.</typeparam>
        /// <param name="uid">Entity UID to modify.</param>
        bool RemoveComponentDeferred<T>(EntityUid uid);

        /// <summary>
        ///     Immediately shuts down a component, but defers the removal and deletion until the end of the tick.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>Returns false if the entity did not have the specified component.</returns>
        bool RemoveComponentDeferred(EntityUid uid, Type type);

        /// <summary>
        ///     Immediately shuts down a component, but defers the removal and deletion until the end of the tick.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="netID">Network ID of the component to remove.</param>
        /// <returns>Returns false if the entity did not have the specified component.</returns>
        bool RemoveComponentDeferred(EntityUid uid, ushort netID, MetaDataComponent? meta = null);

        /// <summary>
        ///     Immediately shuts down a component, but defers the removal and deletion until the end of the tick.
        ///     Throws if the given component does not belong to the entity.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="component">Component to remove.</param>
        void RemoveComponentDeferred(EntityUid uid, IComponent component);

        /// <summary>
        ///     Removes all components from an entity, except the required components.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        void RemoveComponents(EntityUid uid, MetaDataComponent? meta = null);

        /// <summary>
        ///     Removes ALL components from an entity. This includes the required components,
        ///     <see cref="TransformComponent"/> and <see cref="MetaDataComponent"/>. This should ONLY be
        ///     used when deleting an entity.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        void DisposeComponents(EntityUid uid, MetaDataComponent? meta = null);

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <typeparam name="T">Component reference type to check for.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent<T>(EntityUid uid) where T : IComponent;

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <typeparam name="T">Component reference type to check for.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent<T>([NotNullWhen(true)] EntityUid? uid) where T : IComponent;

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="reg">The component registration to check for.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent(EntityUid uid, ComponentRegistration reg);

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent(EntityUid uid, Type type);

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent([NotNullWhen(true)] EntityUid? uid, Type type);

        /// <summary>
        ///     Checks if the entity has a component with a given network ID. This does not check
        ///     if the component is deleted.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netId">Network ID to check for.</param>
        /// <returns>True if the entity has a component with the given network ID, otherwise false.</returns>
        bool HasComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null);

        /// <summary>
        ///     Checks if the entity has a component with a given network ID. This does not check
        ///     if the component is deleted.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netId">Network ID to check for.</param>
        /// <returns>True if the entity has a component with the given network ID, otherwise false.</returns>
        bool HasComponent([NotNullWhen(true)] EntityUid? uid, ushort netId, MetaDataComponent? meta = null);

        /// <summary>
        ///     This method will always return a component for a certain entity, adding it if it's not there already.
        /// </summary>
        /// <param name="uid">Entity to modify.</param>
        /// <typeparam name="T">Component to add.</typeparam>
        /// <returns>The component in question</returns>
        T EnsureComponent<T>(EntityUid uid) where T : IComponent, new();

        /// <summary>
        ///     This method will always return a component for a certain entity, adding it if it's not there already.
        /// </summary>
        /// <param name="uid">Entity to modify.</param>
        /// <param name="component">The output component after being ensured.</param>
        /// <typeparam name="T">Component to add.</typeparam>
        /// <returns>True if the component already existed</returns>
        bool EnsureComponent<T>(EntityUid uid, out T component) where T : IComponent, new();

        /// <summary>
        ///     This method will always return a component for a certain entity, adding it if it's not there already.
        /// </summary>
        /// <param name="entity">Entity to modify.</param>
        /// <typeparam name="T">Component to add.</typeparam>
        /// <returns>True if the component already existed</returns>
        bool EnsureComponent<T>(ref Entity<T?> entity) where T : IComponent, new();

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <typeparam name="T">A trait or type of a component to retrieve.</typeparam>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>The component of Type from the Entity.</returns>
        T GetComponent<T>(EntityUid uid) where T : IComponent;

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>The component of Type from the Entity.</returns>
        IComponent GetComponent(EntityUid uid, CompIdx type);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>The component of Type from the Entity.</returns>
        IComponent GetComponent(EntityUid uid, Type type);

        /// <summary>
        ///     Returns the component with a specific network ID. This does not check
        ///     if the component is deleted.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="netId">Network ID of the component to retrieve.</param>
        /// <returns>The component with the specified network id.</returns>
        IComponent GetComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null);

        /// <summary>
        ///     Returns the component of a specific type, even if it has been marked for deletion.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <returns>The component of Type from the Entity.</returns>
        IComponent GetComponentInternal(EntityUid uid, CompIdx type);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <typeparam name="T">A trait or type of a component to retrieve.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T? component) where T : IComponent?;

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <typeparam name="T">A trait or type of a component to retrieve.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent<T>([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out T? component) where T : IComponent?;

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="reg">The component registration to check for.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, ComponentRegistration reg, [NotNullWhen(true)] out IComponent? component);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, Type type, [NotNullWhen(true)] out IComponent? component);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, CompIdx type, [NotNullWhen(true)] out IComponent? component);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">A trait or component type to check for.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, Type type, [NotNullWhen(true)] out IComponent? component);

        /// <summary>
        ///     Returns the component with a specified network ID. This does not check
        ///     if the component is deleted.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netId">Component Network ID to check for.</param>
        /// <param name="component">Component with the specified network id.</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, ushort netId, [NotNullWhen(true)] out IComponent? component, MetaDataComponent? meta = null);

        /// <summary>
        ///     Returns the component with a specified network ID. This does not check
        ///     if the component is deleted.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netId">Component Network ID to check for.</param>
        /// <param name="component">Component with the specified network id.</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, ushort netId, [NotNullWhen(true)] out IComponent? component, MetaDataComponent? meta = null);

        /// <summary>
        /// Tries to run <see cref="CopyComponents"/> without throwing if the component doesn't exist.
        /// </summary>
        bool TryCopyComponent<T>(
            EntityUid source,
            EntityUid target,
            ref T? sourceComponent,
            [NotNullWhen(true)] out T? targetComp,
            MetaDataComponent? meta = null) where T : IComponent;

        /// <summary>
        /// Tries to run <see cref="CopyComponents"/> without throwing if the components don't exist.
        /// </summary>
        bool TryCopyComponents(EntityUid source, EntityUid target, MetaDataComponent? meta = null, params Type[] sourceComponents);

        /// <summary>
        ///     Copy a single component from source to target entity.
        /// </summary>
        /// <param name="source">The source entity to copy from.</param>
        /// <param name="target">The target entity to copy to.</param>
        /// <param name="sourceComponent">The source component instance to copy.</param>
        /// <param name="component">The copied component if successful.</param>
        /// <param name="meta">Optional metadata of the target entity.</param>
        IComponent CopyComponent(EntityUid source, EntityUid target, IComponent sourceComponent, MetaDataComponent? meta = null);

        /// <summary>
        ///     Copy a single component from source to target entity.
        /// </summary>
        /// <typeparam name="T">The type of component to copy.</typeparam>
        /// <param name="source">The source entity to copy from.</param>
        /// <param name="target">The target entity to copy to.</param>
        /// <param name="sourceComponent">The source component instance to copy.</param>
        /// <param name="component">The copied component if successful.</param>
        /// <param name="meta">Optional metadata of the target entity.</param>
        T CopyComponent<T>(EntityUid source, EntityUid target, T sourceComponent, MetaDataComponent? meta = null) where T : IComponent;

        /// <summary>
        /// Copy multiple components from source to target entity using existing component instances.
        /// </summary>
        /// <param name="source">The source entity to copy from.</param>
        /// <param name="target">The target entity to copy to.</param>
        /// <param name="meta">Optional metadata of the target entity.</param>
        /// <param name="sourceComponents">Array of component instances to copy.</param>
        void CopyComponents(EntityUid source, EntityUid target, MetaDataComponent? meta = null, params IComponent[] sourceComponents);

        /// <summary>
        /// Returns a cached struct enumerator with the specified component.
        /// </summary>
        EntityQuery<TComp1> GetEntityQuery<TComp1>() where TComp1 : IComponent;

        EntityQuery<IComponent> GetEntityQuery(Type type);

        /// <summary>
        ///     Returns ALL component type instances on an entity. A single component instance
        ///     can have multiple component types.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All component types on the Entity.</returns>
        IEnumerable<IComponent> GetComponents(EntityUid uid);

        /// <summary>
        ///     Returns ALL component type instances that are assignable to the specified type.
        ///     A single component instance can have multiple component type instances.
        /// </summary>
        /// <typeparam name="T">A trait or type of a component to retrieve.</typeparam>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All components that are assignable to the specified type.</returns>
        IEnumerable<T> GetComponents<T>(EntityUid uid);

        /// <summary>
        /// Returns the number of components on this entity.
        /// </summary>
        int ComponentCount(EntityUid uid);

        /// <summary>
        ///     Returns ALL networked components on an entity, including deleted ones.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All components that have a network ID.</returns>
        NetComponentEnumerable GetNetComponents(EntityUid uid, MetaDataComponent? meta = null);

        /// <summary>
        ///     Returns ALL networked components on an entity, including deleted ones. Returns null if the entity does
        ///     not exist.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All components that have a network ID.</returns>
        public NetComponentEnumerable? GetNetComponentsOrNull(EntityUid uid, MetaDataComponent? meta = null);

        /// <summary>
        ///     Gets a component state.
        /// </summary>
        /// <param name="eventBus">A reference to the event bus instance.</param>
        /// <param name="component">Component to generate the state for.</param>
        /// <param name="player">The player that is going to receive this state. Null implies that this state is for a replay.</param>
        /// <param name="fromTick">The from tick, which indicates the range of data that must be included for delta-states.</param>
        /// <returns>The component state of the component.</returns>
        IComponentState? GetComponentState(IEventBus eventBus, IComponent component, ICommonSession? player, GameTick fromTick);

        /// <summary>
        ///     Checks if a certain player should get a component state.
        /// </summary>
        /// <param name="eventBus">A reference to the event bus instance.</param>
        /// <param name="component">Component to generate the state for.</param>
        /// <param name="player">The player to generate the state for.</param>
        /// <returns>True if the player should get the component state.</returns>
        bool CanGetComponentState(IEventBus eventBus, IComponent component, ICommonSession player);

        /// <summary>
        /// Returns all instances of a component in an array.
        /// Use sparingly.
        /// </summary>
        (EntityUid Uid, T Component)[] AllComponents<T>() where T : IComponent;

        /// <summary>
        /// Returns an array of all entities that have the given component.
        /// Use sparingly.
        /// </summary>
        Entity<T>[] AllEntities<T>() where T : IComponent;

        /// <summary>
        /// Returns an array of all entities that have the given component.
        /// Use sparingly.
        /// </summary>
        Entity<IComponent>[] AllEntities(Type tComp);

        /// <summary>
        /// Returns an array uids of all entities that have the given component.
        /// Use sparingly.
        /// </summary>
        EntityUid[] AllEntityUids<T>() where T : IComponent;

        /// <summary>
        /// Returns an array uids of all entities that have the given component.
        /// Use sparingly.
        /// </summary>
        EntityUid[] AllEntityUids(Type tComp);

        /// <summary>
        /// Returns all instances of a component in a List.
        /// Use sparingly.
        /// </summary>
        List<(EntityUid Uid, T Component)> AllComponentsList<T>() where T : IComponent;

        /// <summary>
        /// <see cref="ComponentQueryEnumerator"/>
        /// </summary>
        public ComponentQueryEnumerator ComponentQueryEnumerator(ComponentRegistry registry);

        /// <summary>
        /// <see cref="CompRegistryQueryEnumerator"/>
        /// </summary>
        public CompRegistryEntityEnumerator CompRegistryQueryEnumerator(ComponentRegistry registry);

        AllEntityQueryEnumerator<IComponent> AllEntityQueryEnumerator(Type comp);

        AllEntityQueryEnumerator<TComp1> AllEntityQueryEnumerator<TComp1>()
            where TComp1 : IComponent;

        AllEntityQueryEnumerator<TComp1, TComp2> AllEntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent;

        AllEntityQueryEnumerator<TComp1, TComp2, TComp3> AllEntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent;

        AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent;

        EntityQueryEnumerator<TComp1> EntityQueryEnumerator<TComp1>()
            where TComp1 : IComponent;

        EntityQueryEnumerator<TComp1, TComp2> EntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent;

        EntityQueryEnumerator<TComp1, TComp2, TComp3> EntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent;

        EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent;

        /// <summary>
        ///     Returns ALL component instances of a specified type.
        /// </summary>
        /// <typeparam name="T">A trait or type of a component to retrieve.</typeparam>
        /// <returns>All components that have the specified type.</returns>
        IEnumerable<T> EntityQuery<T>(bool includePaused = false) where T: IComponent;

        /// <summary>
        /// Returns the relevant components from all entities that contain the two required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <returns>The pairs of components from each entity that has the two required components.</returns>
        IEnumerable<(TComp1, TComp2)> EntityQuery<TComp1, TComp2>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent;

        /// <summary>
        /// Returns the relevant components from all entities that contain the three required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <typeparam name="TComp3">Third required component.</typeparam>
        /// <returns>The pairs of components from each entity that has the three required components.</returns>
        IEnumerable<(TComp1, TComp2, TComp3)> EntityQuery<TComp1, TComp2, TComp3>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent;

        /// <summary>
        /// Returns the relevant components from all entities that contain the four required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <typeparam name="TComp3">Third required component.</typeparam>
        /// <typeparam name="TComp4">Fourth required component.</typeparam>
        /// <returns>The pairs of components from each entity that has the four required components.</returns>
        IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQuery<TComp1, TComp2, TComp3, TComp4>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent;

        /// <summary>
        ///      Returns ALL component instances of a specified type.
        /// </summary>
        /// <param name="type">A trait or component type to check for.</param>
        /// <param name="includePaused"></param>
        /// <returns>All components that are the specified type.</returns>
        IEnumerable<(EntityUid Uid, IComponent Component)> GetAllComponents(Type type, bool includePaused = false);

        /// <summary>
        ///     Culls all components from the collection that are marked as deleted. This needs to be called often.
        /// </summary>
        void CullRemovedComponents();
    }
}
