using System.Collections.Generic;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// Returns true if the entity only exists on the client.
    /// </summary>
    public bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns the corresponding local <see cref="EntityUid"/>.
    /// </summary>
    public EntityUid ToEntity(NetEntity nEntity);

    /// <summary>
    /// Returns the corresponding local <see cref="EntityUid"/>.
    /// </summary>
    public EntityUid? ToEntity(NetEntity? nEntity);

    /// <summary>
    /// Returns the corresponding <see cref="NetEntity"/> for the local entity.
    /// </summary>
    public NetEntity ToNetEntity(EntityUid uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// Returns the corresponding <see cref="NetEntity"/> for the local entity.
    /// </summary>
    public NetEntity? ToNetEntity(EntityUid? uid, MetaDataComponent? metadata = null);

    /// <summary>
    /// HashSet version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    public HashSet<EntityUid> ToEntitySet(HashSet<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    public List<EntityUid> ToEntityList(List<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    public List<EntityUid> ToEntityList(ICollection<NetEntity> netEntities);

    /// <summary>
    /// List version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    public List<EntityUid?> ToEntityList(List<NetEntity?> netEntities);

    /// <summary>
    /// List version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    EntityUid[] ToEntityArray(NetEntity[] netEntities);

    /// <summary>
    /// List version of <see cref="ToEntity(Robust.Shared.GameObjects.NetEntity)"/>
    /// </summary>
    EntityUid?[] ToEntityArray(NetEntity?[] netEntities);

    /// <summary>
    /// HashSet version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public HashSet<NetEntity> ToNetEntitySet(HashSet<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public List<NetEntity> ToNetEntityList(List<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public List<NetEntity> ToNetEntityList(ICollection<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public List<NetEntity?> ToNetEntityList(List<EntityUid?> entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    NetEntity[] ToNetEntityArray(EntityUid[] entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    NetEntity?[] ToNetEntityArray(EntityUid?[] entities);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates ToNetCoordinates(EntityCoordinates coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="NetCoordinates"/> for the specified local coordinates.
    /// </summary>
    public NetCoordinates? ToNetCoordinates(EntityCoordinates? coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates ToCoordinates(NetCoordinates coordinates);

    /// <summary>
    /// Returns the corresponding <see cref="EntityCoordinates"/> for the specified network coordinates.
    /// </summary>
    public EntityCoordinates? ToCoordinates(NetCoordinates? coordinates);

    public HashSet<EntityCoordinates> ToEntitySet(HashSet<NetCoordinates> netEntities);

    public List<EntityCoordinates> ToEntityList(List<NetCoordinates> netEntities);

    public List<EntityCoordinates> ToEntityList(ICollection<NetCoordinates> netEntities);

    public List<EntityCoordinates?> ToEntityList(List<NetCoordinates?> netEntities);

    public EntityCoordinates[] ToEntityArray(NetCoordinates[] netEntities);

    public EntityCoordinates?[] ToEntityArray(NetCoordinates?[] netEntities);

    public HashSet<NetCoordinates> ToNetCoordinatesSet(HashSet<EntityCoordinates> entities);

    public List<NetCoordinates> ToNetCoordinatesList(List<EntityCoordinates> entities);

    public List<NetCoordinates> ToNetCoordinatesList(ICollection<EntityCoordinates> entities);

    public List<NetCoordinates?> ToNetCoordinatesList(List<EntityCoordinates?> entities);

    public NetCoordinates[] ToNetCoordinatesArray(EntityCoordinates[] entities);

    public NetCoordinates?[] ToNetCoordinatesArray(EntityCoordinates?[] entities);
}
