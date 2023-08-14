using System.Collections.Generic;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <summary>
    /// Inverse lookup for net entities.
    /// Regular lookup uses MetadataComponent.
    /// </summary>
    protected readonly Dictionary<NetEntity, EntityUid> NetEntityLookup = new();

    /// <inheritdoc />
    public virtual bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null)
    {
        return false;
    }

    /// <inheritdoc />
    public EntityUid ToEntity(NetEntity nEntity)
    {
        if (nEntity == NetEntity.Invalid)
            return EntityUid.Invalid;

        return NetEntityLookup.TryGetValue(nEntity, out var entity) ? entity : EntityUid.Invalid;
    }

    /// <inheritdoc />
    public EntityUid? ToEntity(NetEntity? nEntity)
    {
        if (nEntity == null)
            return null;

        return ToEntity(nEntity.Value);
    }

    /// <inheritdoc />
    public NetEntity ToNetEntity(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (uid == EntityUid.Invalid)
            return NetEntity.Invalid;

        // I wanted this to logMissing but it seems to break a loootttt of dodgy stuff on content.
        return MetaQuery.Resolve(uid, ref metadata, false) ? metadata.NetEntity : NetEntity.Invalid;
    }

    /// <inheritdoc />
    public NetEntity? ToNetEntity(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        if (uid == null)
            return null;

        return ToNetEntity(uid.Value, metadata);
    }

    #region NetCoordinates

    /// <inheritdoc />
    public NetCoordinates ToNetCoordinates(EntityCoordinates coordinates)
    {
        return new NetCoordinates(ToNetEntity(coordinates.EntityId), coordinates.Position);
    }

    /// <inheritdoc />
    public NetCoordinates? ToNetCoordinates(EntityCoordinates? coordinates)
    {
        if (coordinates == null)
            return null;

        return new NetCoordinates(ToNetEntity(coordinates.Value.EntityId), coordinates.Value.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates ToCoordinates(NetCoordinates coordinates)
    {
        return new EntityCoordinates(ToEntity(coordinates.NetEntity), coordinates.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates? ToCoordinates(NetCoordinates? coordinates)
    {
        if (coordinates == null)
            return null;

        return new EntityCoordinates(ToEntity(coordinates.Value.NetEntity), coordinates.Value.Position);
    }

    #endregion

    #region Collection helpers

    /// <inheritdoc />
    public HashSet<EntityUid> ToEntitySet(HashSet<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntitySet();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(ToEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> ToEntityList(List<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntityList();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(ToEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> ToEntityList(ICollection<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntityList();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(ToEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid?> ToEntityList(List<NetEntity?> netEntities)
    {
        var entities = new List<EntityUid?>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(ToEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid[] ToEntityArray(NetEntity[] netEntities)
    {
        var entities = new EntityUid[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = ToEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid?[] ToEntityArray(NetEntity?[] netEntities)
    {
        var entities = new EntityUid?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = ToEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetEntity> ToNetEntitySet(HashSet<EntityUid> entities)
    {
        var newSet = _poolManager.GetNetEntitySet();
        newSet.EnsureCapacity(entities.Count);

        foreach (var ent in entities)
        {
            MetaQuery.TryGetComponent(ent, out var metadata);
            newSet.Add(ToNetEntity(ent, metadata));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetEntity> ToNetEntityList(List<EntityUid> entities)
    {
        var netEntities = _poolManager.GetNetEntityList();
        netEntities.EnsureCapacity(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(ToNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity> ToNetEntityList(ICollection<EntityUid> entities)
    {
        var netEntities = _poolManager.GetNetEntityList();
        netEntities.EnsureCapacity(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(ToNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity?> ToNetEntityList(List<EntityUid?> entities)
    {
        var netEntities = new List<NetEntity?>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(ToNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity[] ToNetEntityArray(EntityUid[] entities)
    {
        var netEntities = new NetEntity[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = ToNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity?[] ToNetEntityArray(EntityUid?[] entities)
    {
        var netEntities = new NetEntity?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = ToNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public HashSet<EntityCoordinates> ToEntitySet(HashSet<NetCoordinates> netEntities)
    {
        var entities = new HashSet<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(ToCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> ToEntityList(List<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(ToCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> ToEntityList(ICollection<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(ToCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates?> ToEntityList(List<NetCoordinates?> netEntities)
    {
        var entities = new List<EntityCoordinates?>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(ToCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates[] ToEntityArray(NetCoordinates[] netEntities)
    {
        var entities = new EntityCoordinates[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = ToCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates?[] ToEntityArray(NetCoordinates?[] netEntities)
    {
        var entities = new EntityCoordinates?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = ToCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetCoordinates> ToNetCoordinatesSet(HashSet<EntityCoordinates> entities)
    {
        var newSet = new HashSet<NetCoordinates>(entities.Count);

        foreach (var coordinates in entities)
        {
            newSet.Add(ToNetCoordinates(coordinates));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetCoordinates> ToNetCoordinatesList(List<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(ToNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates> ToNetCoordinatesList(ICollection<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(ToNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates?> ToNetCoordinatesList(List<EntityCoordinates?> entities)
    {
        var netEntities = new List<NetCoordinates?>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(ToNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates[] ToNetCoordinatesArray(EntityCoordinates[] entities)
    {
        var netEntities = new NetCoordinates[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = ToNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates?[] ToNetCoordinatesArray(EntityCoordinates?[] entities)
    {
        var netEntities = new NetCoordinates?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = ToNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    #endregion
}
