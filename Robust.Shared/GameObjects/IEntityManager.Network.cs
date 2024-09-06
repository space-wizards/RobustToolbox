using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    public void DirtyField(EntityUid uid, IComponentDelta delta, string fieldName, MetaDataComponent? metadata = null);

    public void DirtyField<T>(EntityUid uid, T component, string fieldName, MetaDataComponent? metadata = null)
        where T : IComponentDelta;

    /// <summary>
    /// Tries to parse a string as a NetEntity and return the relevant EntityUid.
    /// </summary>
    public bool TryParseNetEntity(string arg, [NotNullWhen(true)] out EntityUid? entity);

    /// <summary>
    /// TryGet version of <see cref="GetEntity"/>
    /// </summary>
    public bool TryGetEntity(NetEntity nEntity, [NotNullWhen(true)] out EntityUid? entity);

    /// <summary>
    /// TryGet version of <see cref="GetEntity"/>
    /// </summary>
    public bool TryGetEntity(NetEntity? nEntity, [NotNullWhen(true)] out EntityUid? entity);

    /// <summary>
    /// Tries to returns the corresponding local <see cref="EntityUid"/> along with the metdata component.
    /// </summary>
    public bool TryGetEntityData(NetEntity nEntity, [NotNullWhen(true)] out EntityUid? entity,
        [NotNullWhen(true)] out MetaDataComponent? meta);

    /// <summary>
    /// TryGet version of <see cref="GetNetEntity"/>
    /// </summary>
    public bool TryGetNetEntity(EntityUid uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null);

    /// <summary>
    /// TryGet version of <see cref="GetNetEntity"/>
    /// </summary>
    public bool TryGetNetEntity(EntityUid? uid, [NotNullWhen(true)] out NetEntity? netEntity,
        MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns true if the entity only exists on the client.
    /// </summary>
    public bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// Tries to get a corresponding <see cref="EntityUid"/> if it exists, otherwise creates an entity for it.
    /// </summary>
    /// <param name="nEntity">The net entity we're trying to resolve.</param>
    /// <param name="T">The type of the component that may need its state handling run later.</param>
    /// <param name="callerEntity">The entity trying to resolve the net entity. This may be flagged for later component state handling.</param>
    public EntityUid EnsureEntity<T>(NetEntity nEntity, EntityUid callerEntity);

    /// <summary>
    /// Tries to get a corresponding <see cref="EntityUid"/> if it exists and nEntity is not null.
    /// </summary>
    public EntityUid? EnsureEntity<T>(NetEntity? nEntity, EntityUid callerEntity);

    /// <summary>
    /// Returns the corresponding local <see cref="EntityUid"/>.
    /// </summary>
    public EntityUid GetEntity(NetEntity nEntity);

    /// <summary>
    /// Returns the corresponding local <see cref="EntityUid"/>.
    /// </summary>
    public EntityUid? GetEntity(NetEntity? nEntity);

    /// <summary>
    /// Returns the corresponding <see cref="NetEntity"/> for the local entity.
    /// </summary>
    public NetEntity GetNetEntity(EntityUid uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns the corresponding <see cref="NetEntity"/> for the local entity.
    /// </summary>
    public NetEntity? GetNetEntity(EntityUid? uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// HashSet version of <see cref="GetEntity"/>
    /// </summary>
    public HashSet<EntityUid> GetEntitySet(HashSet<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="GetEntity"/>
    /// </summary>
    public List<EntityUid> GetEntityList(List<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="GetEntity"/>
    /// </summary>
    public List<EntityUid> GetEntityList(ICollection<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="GetEntity"/>
    /// </summary>
    public List<EntityUid?> GetEntityList(List<NetEntity?> netEntities);

    /// <summary>
    /// List version of <see cref="GetEntity"/>
    /// </summary>
    EntityUid[] GetEntityArray(NetEntity[] netEntities);

    /// <summary>
    /// List version of <see cref="GetEntity"/>
    /// </summary>
    EntityUid?[] GetEntityArray(NetEntity?[] netEntities);

    /// <summary>
    /// Dictionary version of <see cref="GetEntity"/>
    /// </summary>
    Dictionary<EntityUid, T> GetEntityDictionary<T>(Dictionary<NetEntity, T> netEntities);

    /// <summary>
    /// Dictionary version of <see cref="GetEntity"/>
    /// </summary>
    Dictionary<T, EntityUid> GetEntityDictionary<T>(Dictionary<T, NetEntity> netEntities) where T : notnull;

    /// <summary>
    /// HashSet version of <see cref="GetNetEntity"/>
    /// </summary>
    public HashSet<NetEntity> GetNetEntitySet(HashSet<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    public List<NetEntity> GetNetEntityList(List<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    List<NetEntity> GetNetEntityList(IReadOnlyList<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    public List<NetEntity> GetNetEntityList(ICollection<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    public List<NetEntity?> GetNetEntityList(List<EntityUid?> entities);

    /// <summary>
    /// Array version of <see cref="GetNetEntity"/>
    /// </summary>
    NetEntity[] GetNetEntityArray(EntityUid[] entities);

    /// <summary>
    /// Array version of <see cref="GetNetEntity"/>
    /// </summary>
    NetEntity?[] GetNetEntityArray(EntityUid?[] entities);

    /// <summary>
    /// Dictionary version of <see cref="GetNetEntity"/>
    /// </summary>
    Dictionary<NetEntity, T> GetNetEntityDictionary<T>(Dictionary<EntityUid, T> entities);

    /// <summary>
    /// Dictionary version of <see cref="GetNetEntity"/>
    /// </summary>
    Dictionary<T, NetEntity> GetNetEntityDictionary<T>(Dictionary<T, EntityUid> entities) where T : notnull;

    /// <summary>
    /// Dictionary version of <see cref="GetNetEntity"/>
    /// </summary>
    Dictionary<T, NetEntity?> GetNetEntityDictionary<T>(Dictionary<T, EntityUid?> entities) where T : notnull;

    /// <summary>
    /// Dictionary version of <see cref="GetNetEntity"/>
    /// </summary>
    Dictionary<NetEntity, NetEntity> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid> entities);

    /// <summary>
    /// Dictionary version of <see cref="GetNetEntity"/>
    /// </summary>
    Dictionary<NetEntity, NetEntity?> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid?> entities);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates GetNetCoordinates(EntityCoordinates coordinates, MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates? GetNetCoordinates(EntityCoordinates? coordinates, MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates GetCoordinates(NetCoordinates coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates? GetCoordinates(NetCoordinates? coordinates);

    /// <summary>
    /// Tries to get a corresponding <see cref="EntityCoordinates"/> if it exists, otherwise creates an entity for it.
    /// </summary>
    /// <param name="netCoordinates">The net coordinates we're trying to resolve.</param>
    /// <param name="T">The type of the component that may need its state handling run later.</param>
    /// <param name="callerEntity">The entity trying to resolve the net entity. This may be flagged for later component state handling.</param>
    public EntityCoordinates EnsureCoordinates<T>(NetCoordinates netCoordinates, EntityUid callerEntity);

    /// <summary>
    /// Tries to get a corresponding <see cref="EntityCoordinates"/> if it exists and nEntity is not null.
    /// </summary>
    public EntityCoordinates? EnsureCoordinates<T>(NetCoordinates? netCoordinates, EntityUid callerEntity);

    public HashSet<EntityCoordinates> GetEntitySet(HashSet<NetCoordinates> netEntities);

    public List<EntityCoordinates> GetEntityList(List<NetCoordinates> netEntities);

    public HashSet<EntityUid> EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity);

    public List<EntityUid> EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity);

    void EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity, List<EntityUid> entities);

    void EnsureEntityDictionary<TComp, TValue>(Dictionary<NetEntity, TValue> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, TValue> entities);

    void EnsureEntityDictionaryNullableValue<TComp, TValue>(Dictionary<NetEntity, TValue?> netEntities,
        EntityUid callerEntity,
        Dictionary<EntityUid, TValue?> entities);

    void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity> netEntities, EntityUid callerEntity,
        Dictionary<TKey, EntityUid> entities) where TKey : notnull;

    void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity?> netEntities, EntityUid callerEntity,
        Dictionary<TKey, EntityUid?> entities) where TKey : notnull;

    void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, EntityUid> entities);

    void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity?> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, EntityUid?> entities);

    public List<EntityCoordinates> GetEntityList(ICollection<NetCoordinates> netEntities);

    public List<EntityCoordinates?> GetEntityList(List<NetCoordinates?> netEntities);

    public EntityCoordinates[] GetEntityArray(NetCoordinates[] netEntities);

    public EntityCoordinates?[] GetEntityArray(NetCoordinates?[] netEntities);

    public HashSet<NetCoordinates> GetNetCoordinatesSet(HashSet<EntityCoordinates> entities);

    public List<NetCoordinates> GetNetCoordinatesList(List<EntityCoordinates> entities);

    public List<NetCoordinates> GetNetCoordinatesList(ICollection<EntityCoordinates> entities);

    public List<NetCoordinates?> GetNetCoordinatesList(List<EntityCoordinates?> entities);

    public NetCoordinates[] GetNetCoordinatesArray(EntityCoordinates[] entities);

    public NetCoordinates?[] GetNetCoordinatesArray(EntityCoordinates?[] entities);

    /// <summary>
    /// Returns the corresponding local <see cref="EntityUid"/> along with the metdata component.
    /// throws an exception if the net entity does not exist.
    /// </summary>
    (EntityUid, MetaDataComponent) GetEntityData(NetEntity nEntity);
}
