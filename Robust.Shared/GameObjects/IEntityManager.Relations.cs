using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// Assign an entity to have a relation. Clears the passed relation if it's not null.
    /// </summary>
    /// <param name="owner">Owner of the provided <see cref="relation"/> that has a component that stores the reference.</param>
    /// <param name="relation">The relation struct that will hold the reference to <see cref="entity"/>.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in the <see cref="relation"/>.</param>
    /// <param name="dirty">If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.</param>
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
    /// Sets a relation between the <see cref="entity"/> and the <see cref="owner"/>
    /// and adds the entity to a list of <see cref="relations"/>.
    /// </summary>
    /// <param name="owner">Owner of a list of relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="dirty">If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.</param>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <summary>
    /// Sets a relation between the <see cref="entity"/> and the <see cref="owner"/>
    /// and adds the entity to a set of <see cref="relations"/>.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="dirty">If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.</param>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true);

    /// <summary>
    /// Sets a relation between the <see cref="entity"/> and the <see cref="owner"/>
    /// and adds the entity to a dictionary of <see cref="relations"/> together with a <see cref="value"/>.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="value">The value to add paired with the new relation.</param>
    /// <param name="dirty">If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.</param>
    public void AddRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?>? entity,
        T value,
        bool dirty = true);

    /// <summary>
    /// Sets a relation between the <see cref="entity"/> and the <see cref="owner"/>
    /// and adds the entity to a dictionary of <see cref="relations"/> together with a <see cref="key"/>.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entity">An entity that will become related to <see cref="owner"/> and stored in <see cref="relations"/>.</param>
    /// <param name="key">The key to add paired with the new relation.</param>
    /// <param name="dirty">If set to false, will prevent both <see cref="EntityRelationsComponent"/>s from sending to clients.</param>
    public void AddRelation<T>(
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
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    public void AddRelations(
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
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities,
        bool dirty = true);

    /// <summary>
    /// Sets an array of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    /// <param name="entities">An array of entities to add to the relations list.</param>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        bool dirty = true,
        params Entity<EntityRelationsComponent?>?[] entities);

    /// <summary>
    /// Sets an array of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    /// <param name="entities">An array of entities to add to the relations set.</param>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        bool dirty = true,
        params Entity<EntityRelationsComponent?>?[] entities);

    /// <summary>
    /// Sets a dictionary of entities as keys to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entities">A set of entities to add to the relations set.</param>
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    public void AddRelations<T>(
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
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    public void AddRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities,
        bool dirty = true) where T : notnull;

    /// <summary>
    /// Sets a list of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="entities">A list of entities to add to the relations list.</param>
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
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
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
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
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities,
        bool dirty = true);

    /// <summary>
    /// Clears all <see cref="EntityRelation"/>s in the <see cref="relations"/> dictionary and assigns new ones
    /// from the <see cref="entities"/> dictionary.
    /// The cleared relation values are set to <see cref="EntityRelation.Null"/>
    /// if the <see cref="T"/> key doesn't appear in the <see cref="entities"/> dictionary.
    /// The result is a dictionary with merged keys and pairs of relations that may be now empty.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A dictionary of relations to store the result in.</param>
    /// <param name="entities">A dictionary of entities to set into the dictionary.</param>
    /// <param name="dirty"> If set to false, will prevent <see cref="owner"/>'s <see cref="EntityRelationsComponent"/> from sending to clients.</param>
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
        bool dirty = true,
        bool removeKey = false) where T : notnull;

    /// <summary>
    /// Checks if an entity is contained in the specified list of relations.
    /// </summary>
    public bool HasRelation(List<EntityRelation> relations, Entity<EntityRelationsComponent?>? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified set of relations.
    /// </summary>
    public bool HasRelation(HashSet<EntityRelation> relations, Entity<EntityRelationsComponent?>? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified dictionary of relations as a key.
    /// </summary>
    public bool HasRelation<T>(Dictionary<EntityRelation, T> relations, Entity<EntityRelationsComponent?>? entity);

    /// <summary>
    /// Checks if an entity is contained in the specified dictionary of relations as a value.
    /// </summary>
    public bool HasRelation<T>(Dictionary<T, EntityRelation> relations, Entity<EntityRelationsComponent?>? entity) where T : notnull;
}
