using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// Assign an entity to have a relation. Clears the passed relation if it's not null.
    /// </summary>
    /// <param name="owner">
    /// Owner of the provided <see cref="relation"/>, has a component that stores the reference.
    /// </param>
    /// <param name="relation">
    /// The relation struct will hold the reference to <see cref="entity"/>.
    /// </param>
    /// <param name="entity">
    /// An entity that will become related to <see cref="owner"/> and stored in the <see cref="relation"/>.
    /// </param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation relation,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <inheritdoc cref="SetRelation(Entity{EntityRelationsComponent?}, ref EntityRelation, Entity{EntityRelationsComponent?}?, bool)"/>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation? relation,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <summary>
    /// Sets a list of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <summary>
    /// Sets a set of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <summary>
    /// Sets a dictionary of entities as keys to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="value">The value to add paired with the new relation.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?>? entity,
        T value,
        bool dirty = true);

    /// <summary>
    /// Sets a dictionary of entities as values to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="key">The key to add paired with the new relation.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        T key,
        bool dirty = true) where T : notnull;

    /// <summary>
    /// Sets a list of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="entities">A list of entities to add to the relations list.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities,
        bool dirty = true);

    /// <summary>
    /// Sets a set of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entities">A set of entities to add to the relations set.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities,
        bool dirty = true);

    /// <summary>
    /// Sets a dictionary of entities as keys to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entities">A set of entities to add to the relations set.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities,
        bool dirty = true);

    /// <summary>
    /// Sets a dictionary of entities as values to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entities">A set of entities to add to the relations set.</param>
    /// <param name="dirty">
    /// If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.
    /// Use this if you want to make a custom networking setup.
    /// </param>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities,
        bool dirty = true) where T : notnull;

    /// <summary>
    /// Removes all relations to an <see cref="EntityRelation"/>.
    /// This deletes all links in other entities by raising <see cref="EntityRelationDeleteEvent"/>,
    /// making the entity unreferenced by any other components.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during the deletion of an <see cref="EntityRelationsComponent"/> entity.
    /// </remarks>
    public void ClearRelations(EntityRelation relation, EntityRelationsComponent? relations = null, bool dirty = true);

    /// <summary>
    /// Removes all relations to an entity with <see cref="EntityRelationsComponent"/>.
    /// This deletes all links in other entities by raising <see cref="EntityRelationDeleteEvent"/>,
    /// making the entity unreferenced by any other components.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during shutdown of the <see cref="EntityRelationsComponent"/> entity.
    /// </remarks>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, bool dirty = true);

    /// <summary>
    /// Removes a relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// The relation is set to <see cref="EntityRelation.Null"/> after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation relation, bool dirty = true);

    /// <summary>
    /// Removes a relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// The relation is set to null after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation? relation, bool dirty = true);

    /// <summary>
    /// Removes a list of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The list is cleared after the call.
    /// </summary>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations, bool dirty = true);

    /// <summary>
    /// Removes a set of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The set is cleared after the call.
    /// </summary>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations, bool dirty = true);

    /// <summary>
    /// Removes all dictionary keys of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The dictionary is cleared after the call.
    /// </summary>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations, bool dirty = true);

    /// <summary>
    /// Removes all dictionary values relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// All values are set to null after the call.
    /// </summary>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations, bool dirty = true) where T : notnull;

    /// <summary>
    /// Removes a list of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The list is cleared after the call.
    /// </summary>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true);

    /// <summary>
    /// Removes a set of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The set is cleared after the call.
    /// </summary>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true);

    /// <summary>
    /// Removes a specific dictionary key relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// </summary>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true);

    /// <summary>
    /// Clears a specific dictionary value relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// </summary>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true) where T : notnull;

    /// <summary>
    /// Checks if an entity is contained in the specified list of relations.
    /// </summary>
    public bool HasRelation(List<EntityRelation> relations, EntityUid? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified set of relations.
    /// </summary>
    public bool HasRelation(HashSet<EntityRelation> relations, EntityUid? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified dictionary of relations.
    /// </summary>
    public bool HasRelation<T>(Dictionary<EntityRelation, T> relations, EntityUid? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified dictionary of relations.
    /// </summary>
    public bool HasRelation<T>(Dictionary<T, EntityRelation> relations, EntityUid? entity) where T : notnull;
}
