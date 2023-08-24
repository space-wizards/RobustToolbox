using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// TryGet version of <see cref="GetEntity"/>
    /// </summary>
    public bool TryGetEntity(NetEntity nEntity, out EntityUid entity);

    /// <inheritdoc />
    public bool TryGetEntity(NetEntity? nEntity, [NotNullWhen(true)] out EntityUid? entity);

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid uid, out NetEntity netEntity, MetaDataComponent? metadata = null);

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid? uid, [NotNullWhen(true)] out NetEntity? netEntity,
        MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns true if the entity only exists on the client.
    /// </summary>
    public bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null);

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
    public List<NetEntity> GetNetEntityList(ICollection<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    public List<NetEntity?> GetNetEntityList(List<EntityUid?> entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    NetEntity[] GetNetEntityArray(EntityUid[] entities);

    /// <summary>
    /// List version of <see cref="GetNetEntity"/>
    /// </summary>
    NetEntity?[] GetNetEntityArray(EntityUid?[] entities);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates GetNetCoordinates(EntityCoordinates coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates? GetNetCoordinates(EntityCoordinates? coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates GetCoordinates(NetCoordinates coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates? GetCoordinates(NetCoordinates? coordinates);

    public HashSet<EntityCoordinates> GetEntitySet(HashSet<NetCoordinates> netEntities);

    public List<EntityCoordinates> GetEntityList(List<NetCoordinates> netEntities);

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
}