using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// Assign an entity to have a relation.
    /// </summary>
    /// <param name="owner">
    /// Owner of the provided <see cref="relation"/>.
    /// An event will be raised to the owner when the specified <see cref="entity"/> gets deleted.
    /// </param>
    /// <param name="relation">
    /// The relation struct will hold the reference to <see cref="entity"/>.
    /// </param>
    /// <param name="entity">
    /// An entity that will become related to <see cref="owner"/> and stored in the <see cref="relation"/>.
    /// </param>
    public void SetRelation(Entity<EntityRelationsComponent?> owner, ref EntityRelation relation, EntityUid? entity);

    /// <summary>
    /// Sets a list of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A list of relations to store the result in.</param>
    /// <param name="entities">A list of entities to add to the relations list.</param>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, List<EntityRelation> relations, List<EntityUid> entities);

    /// <summary>
    /// Sets a set of entities to have a relation with an <see cref="owner"/> entity.
    /// </summary>
    /// <param name="owner">Owner of all provided relations.</param>
    /// <param name="relations">A set of relations to store the result in.</param>
    /// <param name="entities">A set of entities to add to the relations set.</param>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, HashSet<EntityRelation> relations, HashSet<EntityUid> entities);

    /// <summary>
    /// Removes all relations to an <see cref="EntityRelation"/>.
    /// This deletes all links in other entities by raising <see cref="EntityRelationDeleteEvent"/>,
    /// making the entity unreferenced by any other components.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during the deletion of an <see cref="EntityRelationsComponent"/> entity.
    /// </remarks>
    public void ClearRelations(EntityRelation relation, EntityRelationsComponent? relations = null);

    /// <summary>
    /// Removes all relations to an entity with <see cref="EntityRelationsComponent"/>.
    /// This deletes all links in other entities by raising <see cref="EntityRelationDeleteEvent"/>,
    /// making the entity unreferenced by any other components.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during the deletion of an <see cref="EntityRelationsComponent"/> entity.
    /// </remarks>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent);

    /// <summary>
    /// Removes a relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// The relation is set to <see cref="EntityRelation.Null"/> after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation relation);

    /// <summary>
    /// Removes a relation from an entity with <see cref="EntityRelationsComponent"/>.
    /// The relation is set to null after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation? relation);

    /// <summary>
    /// Removes a list of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The list is cleared after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations);

    /// <summary>
    /// Removes a set of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The set is cleared after the call.
    /// </summary>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations);

    /// <summary>
    /// Removes all dictionary keys of relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// The dictionary is cleared after the call.
    /// </summary>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations);

    /// <summary>
    /// Removes all dictionary values relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// All values are set to null after the call.
    /// </summary>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations) where T : notnull;
}
