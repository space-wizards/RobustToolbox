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
    /// HashSet version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public HashSet<NetEntity> ToNetEntitySet(HashSet<EntityUid> entities);

    /// <summary>
    /// List version of <see cref="ToNetEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.MetaDataComponent?)"/>
    /// </summary>
    public List<NetEntity> ToNetEntityList(List<EntityUid> entities);

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
}
