using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <summary>
    /// Inverse lookup for net entities.
    /// Regular lookup uses MetadataComponent.
    /// </summary>
    protected readonly Dictionary<NetEntity, EntityUid> NetEntityLookup = new(EntityCapacity);

    /// <inheritdoc />
    public virtual bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null)
    {
        return false;
    }

    public bool IsClientSide(NetEntity netEntity)
    {
        return (netEntity._id & NetEntity.ClientEntity) == NetEntity.ClientEntity;
    }

    #region NetEntity

    /// <inheritdoc />
    public bool TryGetEntity(NetEntity nEntity, out EntityUid entity)
    {
        if (nEntity == NetEntity.Invalid)
        {
            entity = EntityUid.Invalid;
            return false;
        }

        return NetEntityLookup.TryGetValue(nEntity, out entity);
    }

    /// <inheritdoc />
    public bool TryGetEntity(NetEntity? nEntity, [NotNullWhen(true)] out EntityUid? entity)
    {
        if (nEntity == null)
        {
            entity = null;
            return false;
        }

        if (TryGetEntity(nEntity.Value, out var went))
        {
            entity = went;
            return true;
        }

        entity = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid uid, out NetEntity netEntity, MetaDataComponent? metadata = null)
    {
        if (uid == EntityUid.Invalid)
        {
            netEntity = NetEntity.Invalid;
            return false;
        }

        // I wanted this to logMissing but it seems to break a loootttt of dodgy stuff on content.
        if (MetaQuery.Resolve(uid, ref metadata, false))
        {
            netEntity = metadata.NetEntity;
            return true;
        }

        netEntity = NetEntity.Invalid;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid? uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null)
    {
        if (uid == null)
        {
            netEntity = null;
            return false;
        }

        if (TryGetNetEntity(uid.Value, out var went, metadata))
        {
            netEntity = went;
            return true;
        }

        netEntity = null;
        return false;
    }

    /// <inheritdoc />
    public virtual EntityUid EnsureEntity<T>(NetEntity nEntity, EntityUid callerEntity)
    {
        // On server we don't want to ensure any reserved entities for later or flag for comp state handling
        // so this is just GetEntity.
        return GetEntity(nEntity);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid? EnsureEntity<T>(NetEntity? nEntity, EntityUid callerEntity)
    {
        if (nEntity == null)
            return null;

        return EnsureEntity<T>(nEntity.Value, callerEntity);
    }

    /// <inheritdoc />
    public EntityUid GetEntity(NetEntity nEntity)
    {
        if (nEntity == NetEntity.Invalid)
            return EntityUid.Invalid;

        return NetEntityLookup.TryGetValue(nEntity, out var entity) ? entity : EntityUid.Invalid;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid? GetEntity(NetEntity? nEntity)
    {
        if (nEntity == null)
            return null;

        return GetEntity(nEntity.Value);
    }

    /// <inheritdoc />
    public NetEntity GetNetEntity(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (uid == EntityUid.Invalid)
            return NetEntity.Invalid;

        // I wanted this to logMissing but it seems to break a loootttt of dodgy stuff on content.
        return MetaQuery.Resolve(uid, ref metadata, false) ? metadata.NetEntity : NetEntity.Invalid;
    }

    /// <inheritdoc />
    public NetEntity? GetNetEntity(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        if (uid == null)
            return null;

        return GetNetEntity(uid.Value, metadata);
    }

    #endregion

    #region NetCoordinates

    /// <inheritdoc />
    public NetCoordinates GetNetCoordinates(EntityCoordinates coordinates, MetaDataComponent? metadata = null)
    {
        return new NetCoordinates(GetNetEntity(coordinates.EntityId, metadata), coordinates.Position);
    }

    /// <inheritdoc />
    public NetCoordinates? GetNetCoordinates(EntityCoordinates? coordinates, MetaDataComponent? metadata = null)
    {
        if (coordinates == null)
            return null;

        return new NetCoordinates(GetNetEntity(coordinates.Value.EntityId, metadata), coordinates.Value.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates GetCoordinates(NetCoordinates coordinates)
    {
        return new EntityCoordinates(GetEntity(coordinates.NetEntity), coordinates.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates? GetCoordinates(NetCoordinates? coordinates)
    {
        if (coordinates == null)
            return null;

        return new EntityCoordinates(GetEntity(coordinates.Value.NetEntity), coordinates.Value.Position);
    }

    /// <inheritdoc />
    public virtual EntityCoordinates EnsureCoordinates<T>(NetCoordinates netCoordinates, EntityUid callerEntity)
    {
        // See EnsureEntity
        return GetCoordinates(netCoordinates);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCoordinates? EnsureCoordinates<T>(NetCoordinates? netCoordinates, EntityUid callerEntity)
    {
        if (netCoordinates == null)
            return null;

        return EnsureCoordinates<T>(netCoordinates.Value, callerEntity);
    }

    #endregion

    #region Collection helpers

    /// <inheritdoc />
    public HashSet<EntityUid> GetEntitySet(HashSet<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntitySet();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> GetEntityList(List<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntityList();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    public HashSet<EntityUid> EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity)
    {
        var entities = _poolManager.GetEntitySet();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity)
    {
        var entities = _poolManager.GetEntityList();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> GetEntityList(ICollection<NetEntity> netEntities)
    {
        var entities = _poolManager.GetEntityList();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid?> GetEntityList(List<NetEntity?> netEntities)
    {
        var entities = new List<EntityUid?>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid[] GetEntityArray(NetEntity[] netEntities)
    {
        var entities = new EntityUid[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid?[] GetEntityArray(NetEntity?[] netEntities)
    {
        var entities = new EntityUid?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetEntity> GetNetEntitySet(HashSet<EntityUid> entities)
    {
        var newSet = _poolManager.GetNetEntitySet();
        newSet.EnsureCapacity(entities.Count);

        foreach (var ent in entities)
        {
            MetaQuery.TryGetComponent(ent, out var metadata);
            newSet.Add(GetNetEntity(ent, metadata));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetEntity> GetNetEntityList(List<EntityUid> entities)
    {
        var netEntities = _poolManager.GetNetEntityList();
        netEntities.EnsureCapacity(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity> GetNetEntityList(ICollection<EntityUid> entities)
    {
        var netEntities = _poolManager.GetNetEntityList();
        netEntities.EnsureCapacity(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity?> GetNetEntityList(List<EntityUid?> entities)
    {
        var netEntities = new List<NetEntity?>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity[] GetNetEntityArray(EntityUid[] entities)
    {
        var netEntities = new NetEntity[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity?[] GetNetEntityArray(EntityUid?[] entities)
    {
        var netEntities = new NetEntity?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public HashSet<EntityCoordinates> GetEntitySet(HashSet<NetCoordinates> netEntities)
    {
        var entities = new HashSet<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> GetEntityList(List<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> GetEntityList(ICollection<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates?> GetEntityList(List<NetCoordinates?> netEntities)
    {
        var entities = new List<EntityCoordinates?>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates[] GetEntityArray(NetCoordinates[] netEntities)
    {
        var entities = new EntityCoordinates[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates?[] GetEntityArray(NetCoordinates?[] netEntities)
    {
        var entities = new EntityCoordinates?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetCoordinates> GetNetCoordinatesSet(HashSet<EntityCoordinates> entities)
    {
        var newSet = new HashSet<NetCoordinates>(entities.Count);

        foreach (var coordinates in entities)
        {
            newSet.Add(GetNetCoordinates(coordinates));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetCoordinates> GetNetCoordinatesList(List<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates> GetNetCoordinatesList(ICollection<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates?> GetNetCoordinatesList(List<EntityCoordinates?> entities)
    {
        var netEntities = new List<NetCoordinates?>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates[] GetNetCoordinatesArray(EntityCoordinates[] entities)
    {
        var netEntities = new NetCoordinates[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates?[] GetNetCoordinatesArray(EntityCoordinates?[] entities)
    {
        var netEntities = new NetCoordinates?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    #endregion
}
